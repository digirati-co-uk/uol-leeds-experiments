// This does the same thing as the create-deposit example, but without the extra comments

import {APIRequestContext, expect} from "@playwright/test";
import {ensurePath, getS3Client, uploadFile} from "./common-utils";

export async function createDigitalObject(request: APIRequestContext, baseURL: string){

    const digitalPreservationParent = `/testing/update-examples/${new Date().toISOString()}`;
    await ensurePath(digitalPreservationParent, request)
    const preservedDigitalObjectUri = `${baseURL}/repository${digitalPreservationParent}/MS-10315`;
    const newDepositResp = await request.post('/deposits', {
        data: {
            digitalObject: preservedDigitalObjectUri,
            submissionText: "Creating a new deposit to demonstrate updates"
        }
    })
    const newDeposit = await newDepositResp.json();
    const sourceDir = 'samples/10315s/';
    const files = [
        '10315.METS.xml',
        'objects/372705s_001.jpg',
        'objects/372705s_002.jpg',
        'objects/372705s_003.jpg',
        'objects/372705s_004.jpg'
    ];
    const s3Client = getS3Client();
    for (const file of files) {
        await uploadFile(s3Client, newDeposit.files, sourceDir + file, file)
    }
    console.log("Execute the diff in one operation, without fetching it first (see RFC)")
    // This is a shortcut, a variation on the mechanism shown in create-deposit.spec.ts
    const executeImportJobReq = await request.post(newDeposit['@id'] + '/importJobs', {
        data: { "@id": newDeposit['@id'] + '/importJobs/diff' }
    });
    let importJobResult = await executeImportJobReq.json();
    await expect.poll(async () => {
        const ijrReq = await request.get(importJobResult['@id']);
        const ijr = await ijrReq.json();
        return ijr.status;
    }, {
        intervals: [2000], // every 2 seconds
        timeout: 60000 // allow 1 minute to complete
    }).toMatch(/completed.*/);
    return preservedDigitalObjectUri;
}