import {expect, test} from '@playwright/test';
import { createDigitalObject } from './quick-prep.spec'
import {getS3Client, getShortTimestamp, listKeys, uploadFile} from "./common-utils";
import {ListObjectsV2Command} from "@aws-sdk/client-s3";

// Create a new deposit by exporting a version


test.describe('Update a Digital Object without exporting anything first', () => {


    test('editing-mets', async ({request, baseURL}) => {

        // Set a very long timeout so you can debug on breakpoints or whatnot.
        test.setTimeout(1000000);

        // Background prep
        console.log("Starting condition is a Preserved Digital Object in the repository - we'll make one for this test");
        const digitalObjectUri = await createDigitalObject(request, baseURL);
        console.log("Created " + digitalObjectUri);

        console.log("Create a new Deposit - NOT an export:");
        console.log("POST /deposits");
        const newDepositResp = await request.post('/deposits', {
            data: {
                digitalObject: digitalObjectUri,
                submissionText: "Creating a new deposit to demonstrate updates without exporting"
            }
        })
        const newDeposit = await newDepositResp.json();
        console.log("New Deposit created:");
        console.log(newDeposit);
        console.log("----");

        console.log("Now upload the changed file - just the METS and not 5GB of tiffs!")
        const s3Client = getS3Client();
        await uploadFile(s3Client, newDeposit.files, 'samples/10315s-tweaked-METS/10315.METS.xml', '10315.METS.xml');

        console.log("----");
        console.log("Now build our own import job from scratch");
        const importJobId = "https://digirati.com/importJobs/job-" + getShortTimestamp();
        console.log("We can also mint whatever id we like for this job:")
        console.log(`"@id": "${importJobId}"`);
        console.log("This time we have to add the sha256 ourselves because we are not asking the server to build the import job")
        const importJob = {
            '@id': importJobId,
            type: 'ImportJob',
            deposit: newDeposit['@id'],
            digitalObject: digitalObjectUri,
            containersToAdd: [],
            binariesToAdd: [],
            containersToDelete: [],
            binariesToDelete: [],
            binariesToPatch: [
                {
                    '@id': digitalObjectUri + '/10315.METS.xml',
                    name: '10315.METS.xml',
                    contentType: 'application/xml',
                    partOf: digitalObjectUri,
                    digest: 'c033d56b1c756f718233893ca735a2eb1add86fb4200a99f30c93deb5ffe6adf'
                }
            ]
        };
        console.log(importJob);

        const executeJobUri = newDeposit['@id'] + '/importJobs';
        console.log("Now execute the import job...");
        console.log("POST " + executeJobUri);
        const executeImportJobReq = await request.post(executeJobUri, {
            data: importJob
        });
        let importJobResult = await executeImportJobReq.json();
        console.log(importJobResult);
        expect(importJobResult).toEqual(expect.objectContaining({
            type: 'ImportJobResult',
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
                ])
        }));
    })
});


// maybe you have these files handy already
// replace jpeg 2 with another binary, same name
// sha256 changes in METS but that's all
// => v2

// then another job - add a fifth JPEG
// => v3