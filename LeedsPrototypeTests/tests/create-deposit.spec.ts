import {APIRequestContext, expect, test} from '@playwright/test';
import {PutObjectCommand, S3Client} from "@aws-sdk/client-s3";
import {fromIni} from '@aws-sdk/credential-providers';
import {parseS3Url} from 'amazon-s3-url'
import {readFileSync} from "fs";

// Scenario:
// A completely new digital object / package / book / etc.
// Someone has started working on it in Goobi, but it hasn't ever been saved to Preservation.
// Now it's time to commit it to Preservation.
// (later we'll make further changes to it).

// A Deposit is the mechanism to get files in and out of Preservation:
// https://github.com/uol-dlip/docs/blob/main/rfcs/003-preservation-api.md#deposit
test.describe('Create a deposit and put some files in it', () => {

    let newDeposit = null;

    test('create-deposit', async ({request}) => {

        // Set a very long timeout so you can debug on breakpoints or whatnot.
        test.setTimeout(1000000);

        // Before we start, we want to make sure we have a "folder" in the Digital Preservation Repository to
        // save Digital Objects in. You might want to save 1000s of digital objects under the same
        // parent location - but that location needs to exist! Goobi isn't the only user of the repository.
        // And also, Goobi can create child structure.
        // This will be a no-op except the very first time.
        await ensurePath("/testing/digitised", request);

        // We want to have a new WORKING SPACE - a _Deposit_
        // So we ask for one:
        const depositReq = await request.post('/deposits');
        expect(depositReq.status()).toBe(201);
        newDeposit = await depositReq.json();
        // https://github.com/uol-dlip/docs/blob/main/rfcs/003-preservation-api.md#deposit
        console.log(newDeposit);

        // this deposit could be used for creating a new DigitalObject in
        // the repository, or for updating an existing one.
        // Here we're eventually going to use it to create a new DigitalObject.
        // And if we already know the URI in Preservation that we want that digital object to live at,
        // we can supply it in the initial POST. But we don't have to.
        // (see a later variant where we do supply the `digitalObject` initially)

        // The `files` property of the created deposit gives us an S3 URI
        // from which we can obtain a bucket name and key prefix.
        // It is assumed that, as a known API user, the returned S3 location
        // will be one we can already read and write to.
        // For example, when Goobi asks for a Deposit, the system will
        // return a key within a dedicated bucket set up for Goobi's use in advance.
        // Goobi will already have credentials to interact with this bucket.
        expect(newDeposit.files).toMatch(/s3:\/\/.*/);

        // Now we'll upload the files for a very simple object.
        // We can spend as long as we like here - seconds, as below.
        // Or we can leave this "open" for days, updating files as we like, in multiple
        // separate operations.
        // At no point in uploading files are we interacting with the Preservation API.
        // Only pure AWS S3 APIs.
        // For simplicity, we're going to use the same relative paths in the Deposit as we have locally on disk.
        // We don't HAVE to do this, they can take any layout in the bucket, as long as:
        //  - they are all paths that *start with* deposit.files (an s3 URI)
        //  - the paths conform to the reduced character set (likely a-zA-Z0-9, and -._)
        // The second point is not enforced here because this is
        // using whatever AWS S3 library you like. It is enforced later.
        const sourceDir = 'samples/10315s/';
        const files = [
            '10315.METS.xml',             // we don't need to tell the API that this is the METS file, it will deduce it
            'objects/372705s_001.jpg',
            'objects/372705s_002.jpg',
            'objects/372705s_003.jpg',
            'objects/372705s_004.jpg'
        ];
        const s3Client = getS3Client();
        for (const file of files) {
            await uploadFile(s3Client, newDeposit.files, sourceDir + file, file)
        }

        // Now we have uploaded our files. The next step is to create in ImportJob
        // https://github.com/uol-dlip/docs/blob/main/rfcs/003-preservation-api.md#importjob
        // We could do this manually, for the files we have just placed in the Deposit.
        // A simpler method for this initial save to preservation is to ask the API to generate
        // an ImportJob for us, from the difference between the digital object in Preservation,
        // and the file in the Deposit.
        // However, we need to provide one additional piece of information:
        let preservedDigitalObjectUri = "https://localhost:7169/repository/testing/digitised/MS-10315";

        // To allow this to be run multiple times without conflicts, I'm going to append a timestamp to this URI.
        // WE WOULD NOT DO THAT IN A REAL SCENARIO!
        preservedDigitalObjectUri += getShortTimestamp();

        const depositWithDestination = await request.patch(newDeposit["@id"], {
            data: {
                digitalObject: preservedDigitalObjectUri,
                submissionText: "You can write what you like here"
            }
        });
        expect(await depositWithDestination.json()).toEqual(expect.objectContaining({
            "@id": newDeposit["@id"],  // verify that it's the same deposit!
            digitalObject: preservedDigitalObjectUri
        }));
        // We could have provided this information in the initial POST to create the deposit.
        // I suspect Goobi will know at the start where this should live in Preservation.
        // But some other scenarios might not know that during the initial assembly of files stage.

        // Now we can get the API to generate an ImportJob for us:
        const diffJobGeneratorUri = newDeposit['@id'] + '/importJobs/diff';
        const diffReq = await request.get(diffJobGeneratorUri);
        const diffImportJob = await diffReq.json();
        console.log(diffImportJob);

        // You could edit diffImportJob here, e.g., to remove some files, change names.
        // Notice that the server has used information in the METS as well as the S3 layout.
        //  - It has extracted checksums from the METS
        //  - It has extracted the "real" file names from METS
        //  - It has extracted the name of the Archival Group from the METS
        //  - (it would also extract real Container names too)

        // We will just execute the job as-is, by POSTing it:
        const executeImportJobReq = await request.post(newDeposit['@id'] + '/importJobs', {
            data: diffImportJob
        });

        // Not shown - assign a name to the digital object in the initial creation importJob
        // Instead we will look for a name in the METS file.

        let importJobResult = await executeImportJobReq.json();
        console.log(importJobResult);
        expect(importJobResult).toEqual(expect.objectContaining({
            "@id": expect(String).not.toEqual(diffJobGeneratorUri),
            type: 'ImportJobResult',
            originalImportJobId: diffJobGeneratorUri,
            status: 'waiting',
            digitalObject: preservedDigitalObjectUri
        }));
        // There is a way of executing a diff import job in one step, without having to see the body
        // - see https://github.com/uol-dlip/docs/blob/main/rfcs/003-preservation-api.md#execute-import-job
        // but the above allows us to see what we are about to ask for.
        // It also allows us to override the name or other aspects.

        // We could now go away and do something else, as this job might in a long queue.
        // For this test we'll just wait for it to complete - which means that the status is
        // either "completed" or "completedWithErrors",
        await expect.poll(async () => {
            const ijrReq = await request.get(importJobResult['@id']);
            const ijr = await ijrReq.json();
            return ijr.status;
        }, {
            timeout: 2000 // wait 2 seconds each time
        }).toMatch(/completed.*/);

        // Now we should have a preserved digital object in the repository:
        const digitalObjectReq = await request.get(preservedDigitalObjectUri);
            expect(digitalObjectReq.ok()).toBeTruthy();
            expect(await digitalObjectReq.json()).toContainEqual(expect.objectContaining({
                '@id': preservedDigitalObjectUri,
                type: 'DigitalObject',



        }));










    });
});



function getS3Client() {
    return new S3Client({
        region: "eu-west-1",
        credentials: fromIni({profile: 'uol'})
    });
}

// This isn't using any of the custom Leeds API at all.
// All file transfer is done using AWS APIs, into S3 buckets.
// You don't even need to set the SHA256 checksum on the uploaded file,
// if that checksum is provided in a METS file, allowing multiple
// ways to get the content into S3
async function uploadFile(
    s3: S3Client,
    depositUri: string,
    localFilePath: string,
    relativePathInDigitalObject: string) {

    const s3Url = parseS3Url(depositUri);

    // be forgiving of joining paths...
    const key = s3Url.key.endsWith('/') ? s3Url.key.slice(0,-1) : s3Url.key;
    const path = relativePathInDigitalObject.startsWith('/') ? relativePathInDigitalObject.slice(1) : relativePathInDigitalObject;
    const pathInDeposit = key + '/' + path;

    const putCmd = new PutObjectCommand({
        Bucket: s3Url.bucket,
        Key: pathInDeposit,
        Body: readFileSync(localFilePath)
        // Note that we don't need to set this if the METS file provides it:
        // ChecksumAlgorithm: "SHA256"
        // But if you DO provide this information in S3 metadata, we will validate it against the METS file.
    });

    await s3.send(putCmd);
}

// This is purely for demo purposes and would be no part of a real application!!
// Its purpose is to produce a short string with very small likelihood of collisions.
function getShortTimestamp(){
    const date = new Date();
    const dayOfYear = (Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) - Date.UTC(date.getFullYear(), 0, 0)) / 24 / 60 / 60 / 1000;
    const secondOfDay = date.getHours() * 3600 + date.getMinutes() * 60 + date.getSeconds();
    return `-${String(dayOfYear).padStart(3, '0')}-${String(secondOfDay).padStart(5, '0')}`
}

async function ensurePath(path: string, request: APIRequestContext) {
    const parts = path.split('/');
    let buildPath = "/repository";
    for (const part of parts) {
        if(part){
            buildPath += '/' + part;
            const resourceResp = await request.get(buildPath);
            if(resourceResp.status() == 404){
                // This is always a container, you can't create other kinds of resource outside of a deposit
                const containerResp = await request.post(buildPath);
            }
            // ignore other status codes for now
        }
    }
}
