import {expect, test} from '@playwright/test';
import { createDigitalObject } from './quick-prep.spec'
import {getS3Client, getShortTimestamp, listKeys, uploadFile, waitForStatus} from "./common-utils";
import {ListObjectsV2Command} from "@aws-sdk/client-s3";

// Create a new deposit by exporting a version


test.describe('Update a Digital Object without exporting anything first', () => {


    test('no-exporting', async ({request, baseURL}) => {

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
        console.log("This time we have to add the sha256 ourselves because we are not asking the server to build the import job");
        // TODO: do we want to require that? As long as there's a METS file in the importJob, the server can use it to look for SHA256 hashes
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
                    '@id': digitalObjectUri + '/10315.METS.xml',           // TODO - you shouldn't need BOTH the full ID and the partOf
                    name: '10315.METS.xml',
                    contentType: 'application/xml',
                    partOf: digitalObjectUri,                              // TODO - you shouldn't need BOTH the full ID and the partOf
                    digest: 'c033d56b1c756f718233893ca735a2eb1add86fb4200a99f30c93deb5ffe6adf'    // Likely not require this if METS in payload
                }
            ]
        };
        console.log(importJob);

        const executeJobUri = newDeposit['@id'] + '/importJobs';
        console.log("Now execute the import job...");
        console.log("POST " + executeJobUri);
        console.log("If this fails, check that the sha256 of your local 10315.METS.xml matches the digest above - may be line-ending issue from GitHub");
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
        await waitForStatus(importJobResult['@id'], /completed.*/, request);
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
        console.log("----");


        console.log("Now what if we want to add a fifth JPEG - this is the same as the exporting example, but _without the export_");
        // TODO: OK to use same deposit, or should we create a new one now that the first deposit has been used to create a new version?
        console.log("We are going to add the same extra image as in the exporting example.")
        const sourceDir = 'samples/10315s-add-jpg/';
        const files = [
            '10315.METS.xml',
            'objects/IbqX6-fA.jpg'
        ];
        for (const file of files) {
            await uploadFile(s3Client, newDeposit.files, sourceDir + file, file)
        }
        console.log("And again, generate the import job ourselves, rather than via a diff by the server.")
        console.log("This gives us complete control and allows lightweight updates");
        const importJobId2 = "https://digirati.com/importJobs/job-" + getShortTimestamp();
        const importJob2 = {
            '@id': importJobId2,
            type: 'ImportJob',
            deposit: newDeposit['@id'],
            digitalObject: digitalObjectUri,
            containersToAdd: [],
            binariesToAdd: [
                {
                    '@id': digitalObjectUri + '/objects/IbqX6-fA.jpg',
                    name: 'IbqX6-fA.jpg',
                    contentType: 'image/jpeg',
                    partOf: digitalObjectUri,
                    digest: '5d235e69c795ec73fd45ed5eb9f75be7f0075abccd4ee0b622bfc3c898379710'    // Likely not require this if METS in payload
                }
            ],
            containersToDelete: [],
            binariesToDelete: [],
            binariesToPatch: [
                {
                    '@id': digitalObjectUri + '/10315.METS.xml',
                    name: '10315.METS.xml',
                    contentType: 'application/xml',
                    partOf: digitalObjectUri,
                    digest: 'b9f622c1cf1a4af8f7c353d2afca0e0f89f26b679e2de8a29450479663025bd8'    // Likely not require this if METS in payload
                }
            ]
        };
        console.log(importJob2);
        console.log("-----");

        console.log("Now execute the SECOND import job...");
        console.log("POST " + executeJobUri);
        const executeImportJobReq2 = await request.post(executeJobUri, {
            data: importJob2
        });
        let importJobResult2 = await executeImportJobReq2.json();
        console.log(importJobResult2);
        expect(importJobResult2).toEqual(expect.objectContaining({
            type: 'ImportJobResult',
            status: 'waiting',
            digitalObject: digitalObjectUri
        }));
        console.log("----");

        console.log("... and poll it until it is either complete or completeWithErrors...");
        await waitForStatus(importJobResult2['@id'], /completed.*/, request);
        console.log("----");

        console.log("Now request the digital object yet again:");
        console.log("GET " + digitalObjectUri);
        const digitalObjectReq3 = await request.get(digitalObjectUri);

        expect(digitalObjectReq3.ok()).toBeTruthy();
        const digitalObject3 = await digitalObjectReq3.json();
        console.log("We expect to see THREE versions available in this object")
        console.log(digitalObject3);

        expect(digitalObject3).toEqual(expect.objectContaining({
            '@id': digitalObjectUri,
            type: 'DigitalObject',
            name: '[Example title]', // This will have been read from the METS file  <mods:title>
            version: expect.objectContaining({name: 'v3'}),  // and we expect it to be at version 2
            versions: expect.arrayContaining(
                [
                    expect.objectContaining({name:'v1'}),
                    expect.objectContaining({name:'v2'}),
                    expect.objectContaining({name:'v3'})
                ]
            )
        }));
        console.log("----");
    })
});


// maybe you have these files handy already
// replace jpeg 2 with another binary, same name
// sha256 changes in METS but that's all
// => v2

// then another job - add a fifth JPEG
// => v3