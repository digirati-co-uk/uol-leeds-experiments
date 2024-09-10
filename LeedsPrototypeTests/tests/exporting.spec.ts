import {expect, test} from '@playwright/test';
import { createDigitalObject } from './quick-prep.spec'
import {getS3Client, listKeys, uploadFile} from "./common-utils";
import {ListObjectsV2Command} from "@aws-sdk/client-s3";

// Create a new deposit by exporting a version


test.describe('Export an existing Digital Object and make changes to it, then create a new version in Digital Preservation', () => {


    test('exporting-and-adding-a-file', async ({request, baseURL}) => {

        // Set a very long timeout so you can debug on breakpoints or whatnot.
        test.setTimeout(1000000);

        // Background prep
        console.log("Starting condition is a Preserved Digital Object in the repository - we'll make one for this test");
        const digitalObjectUri = await createDigitalObject(request, baseURL);
        console.log("Created " + digitalObjectUri);

        // The demonstration
        // Assumes we no longer have the original deposit that made the digital object in the first place
        console.log("An hour later, or 10 years later, we want to take a look at the files and possibly edit them.");
        console.log("We could ask for a specific version, but we'll omit the version property to get the most recent (which should be v1 here)")
        const exportResp = await request.post('/deposits/export', {
            data: {
                digitalObject: digitalObjectUri
            }
        });
        const exportDeposit = await exportResp.json();
        expect(exportDeposit.files).toMatch(/s3:\/\/.*/);
        console.log("The files for " + digitalObjectUri + " have been placed under " + exportDeposit.files);

        const s3Client = getS3Client();
        await listKeys(s3Client, exportDeposit.files);

        console.log("We realised we missed a page all those years ago, so we need to add a new image and also modify the METS file.")
        const sourceDir = 'samples/10315s-add-jpg/';
        const files = [
            '10315.METS.xml',
            'objects/IbqX6-fA.jpg'
        ];
        for (const file of files) {
            await uploadFile(s3Client, exportDeposit.files, sourceDir + file, file)
        }

        console.log("Now generate an importJob from a diff");
        const diffJobGeneratorUri = exportDeposit['@id'] + '/importJobs/diff';
        console.log("GET " + diffJobGeneratorUri);
        const diffReq = await request.get(diffJobGeneratorUri);

        const diffImportJob = await diffReq.json();
        console.log(diffImportJob);
        console.log("----");
        expect(diffImportJob).toEqual(expect.objectContaining({
            '@id': diffJobGeneratorUri,
            digitalObject: digitalObjectUri,
            sourceVersion: expect.objectContaining({
                name: 'v1'
            }),
            containersToAdd: [],
            containersToDelete: [],
            binariesToDelete: [],
            binariesToAdd: expect.arrayContaining(
                [
                    expect.objectContaining({'@id': expect.stringContaining('/objects/IbqX6-fA.jpg')})
                ]),
            binariesToPatch: expect.arrayContaining(
                [
                    expect.objectContaining({'@id': expect.stringContaining('/10315.METS.xml')})
                ]),
        }));
        expect(diffImportJob.binariesToAdd.length).toEqual(1);

        // ### API INTERACTION ###
        const executeJobUri = exportDeposit['@id'] + '/importJobs';
        console.log("Now execute the import job...");
        console.log("POST " + executeJobUri);
        const executeImportJobReq = await request.post(executeJobUri, {
            data: diffImportJob
        });
        let importJobResult = await executeImportJobReq.json();
        console.log(importJobResult);
        expect(importJobResult).toEqual(expect.objectContaining({
            type: 'ImportJobResult',
            originalImportJobId: diffJobGeneratorUri,
            status: 'waiting',
            digitalObject: digitalObjectUri
        }));
        console.log("----");

        console.log("... and poll it until it is either complete or completeWithErrors...");
        await expect.poll(async () => {

            // ### API INTERACTION ###
            console.log("GET " + importJobResult['@id']);
            const ijrReq = await request.get(importJobResult['@id']);

            const ijr = await ijrReq.json();
            console.log("status: " + ijr.status);
            return ijr.status;
        }, {
            intervals: [2000], // every 2 seconds
            timeout: 60000 // allow 1 minute to complete
        }).toMatch(/completed.*/);
        console.log("----");

        // Now we should have an UPDATED digital object in the repository:

        // ### API INTERACTION ###
        console.log("Now request the digital object URI we made earlier:");
        console.log("GET " + digitalObjectUri);
        const digitalObjectReq = await request.get(digitalObjectUri);

        expect(digitalObjectReq.ok()).toBeTruthy();
        const digitalObject = await digitalObjectReq.json();
        console.log("We expect to see two versions available in this object")
        console.log(digitalObject);

        expect(digitalObject).toEqual(expect.objectContaining({
            '@id': digitalObjectUri,
            type: 'DigitalObject',
            name: '[Example title]', // This will have been read from the METS file  <mods:title>
            version: expect.objectContaining({name: 'v2'}),  // and we expect it to be at version 2
            versions: expect.arrayContaining(
                [
                    expect.objectContaining({name:'v1'}),
                    expect.objectContaining({name:'v2'})
                ]
            ),
            binaries: expect.arrayContaining(
                [
                    // and it has a METS file in the root
                    expect.objectContaining({'@id': expect.stringContaining('10315.METS.xml')})
                ]),
            containers: expect.arrayContaining(
                [
                    // and an objects folder with 4 JPEGs in it
                    expect.objectContaining(
                        {
                            type: 'Container',
                            name: 'objects',
                            binaries: expect.arrayContaining(
                                [
                                    expect.objectContaining({'@id': expect.stringContaining('/objects/372705s_001.jpg')}),
                                    expect.objectContaining({'@id': expect.stringContaining('/objects/372705s_002.jpg')}),
                                    expect.objectContaining({'@id': expect.stringContaining('/objects/372705s_003.jpg')}),
                                    expect.objectContaining({'@id': expect.stringContaining('/objects/372705s_004.jpg')}),
                                    expect.objectContaining({'@id': expect.stringContaining('/objects/IbqX6-fA.jpg')}),
                                ]
                            )
                        }
                    )
                ])
        }));
    })
});