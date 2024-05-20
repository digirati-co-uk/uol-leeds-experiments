import os
from posixpath import join

import boto3
import click

bucket_name = "uol-expts-staging-01"
session = boto3.Session(profile_name='uol')
s3 = session.client('s3')


@click.command()
@click.option("--folder", default="c:\\temp\\uol\\payloads\\")
@click.option("--deposit", required=True)
def handle_request(folder: str, deposit: str):
    root_key = f"deposits/dev/{deposit}/"
    upload_folder(folder, root_key)


def upload_folder(folder: str, root_key: str):
    # write all keys
    written_keys = []
    for root, dirs, files in os.walk(folder):
        wa = root.replace(folder, root_key).replace("\\", "/")
        for file in files:
            full_path = os.path.join(root, file)
            s3_key = join(wa, file)
            print(f"uploading {full_path} to {s3_key}")
            written_keys.append(s3_key)

            response = s3.put_object(Body=open(full_path, 'rb'),
                                     Bucket=bucket_name,
                                     ChecksumAlgorithm='SHA256',
                                     Key=s3_key)

    # now check existing keys and remove any that weren't written
    list_response = s3.list_objects_v2(
        Bucket=bucket_name,
        Prefix=root_key
    )
    if list_response['IsTruncated']:
        print("list response is truncated. This script doesn't handle that")

    for content in list_response['Contents']:
        if content['Key'] not in written_keys:
            print(f"deleting {content['Key']}")
            s3.delete_object(Bucket=bucket_name, Key=content['Key'])
        else:
            print(f"skipping {content['Key']} as it's been written")


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    handle_request()
