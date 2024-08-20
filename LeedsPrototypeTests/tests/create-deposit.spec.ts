import {test, expect} from '@playwright/test';
import { S3Client, PutObjectCommand } from "@aws-sdk/client-s3";
import { fromIni } from '@aws-sdk/credential-providers';
import { parseS3Url } from 'amazon-s3-url'
import { readFileSync } from "fs";

test.describe('Create a deposit and put some files in it', () => {

    let createdDeposit = null;

    test('create-deposit', async ({request}) => {

        test.setTimeout(120000);

        // We want to have a new WORKING SPACE - a "deposit"
        // So we ask for one:
        const depositReq = await request.post('/deposits');
        expect(depositReq.status()).toBe(201);

        createdDeposit = await depositReq.json();
        // this deposit could be used for creating a new DigitalObject in
        // the repository, or for updating an existing one.

        // The files property of the created deposit gives us an S3 URI
        // from which we can infer the bucket name and key prefix we must use.
        // It is assumed that, as a known API user, the returned S3 location
        // will be one we can already read and write to.
        // For example, when Goobi asks for a Deposit, the system will
        // return a key within a dedicated bucket set up for Goobi's use in advance.
        // Goobi will already have credentials to interact with this bucket.
        expect(createdDeposit.files).toMatch(/s3:\/\/.*/);

        const s3 = new S3Client({
            region: "eu-west-1",
            credentials: fromIni({ profile: 'uol' })
        });

        const sourceDir = 'samples/10315s/';
        const files = [
            '10315.METS.xml',
            'objects/372705s_001.jpg',
            'objects/372705s_002.jpg',
            'objects/372705s_003.jpg',
            'objects/372705s_004.jpg'
        ];
        for (const file of files) {
            await uploadFile(s3, createdDeposit.files, sourceDir + file, file)
        }
    });
});


async function uploadFile(
    s3: S3Client,
    depositUri: string,
    file: string,
    relativePath: string) {

    const s3Url = parseS3Url(depositUri);

    // be forgiving of joining paths...
    const key = s3Url.key.endsWith('/') ? s3Url.key.slice(0,-1) : s3Url.key;
    const path = relativePath.startsWith('/') ? relativePath.slice(1) : relativePath;
    const pathInDeposit = key + '/' + path;

    const putCmd = new PutObjectCommand({
        Bucket: s3Url.bucket,
        Key: pathInDeposit,
        Body: readFileSync(file)
        // Note that we don't need to set this if the METS file provides it:
        // ChecksumAlgorithm: "SHA256"
    });

    await s3.send(putCmd);
}