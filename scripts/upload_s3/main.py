import os
from posixpath import join

import boto3
import click
from botocore.retries import bucket

bucket = "uol-expts-staging-01"
session = boto3.Session(profile_name='uol')
s3 = session.client('s3')


@click.command(context_settings=dict(ignore_unknown_options=True))
@click.option("--folder", "-f", required=True)
@click.option("--root-key", "-r", required=True)
def handle_request(folder: str, root_key: str):
    upload_folder(folder, root_key)


def upload_folder(folder: str, root_key: str):
    for root, dirs, files in os.walk(folder):
        wa = root.replace(folder, root_key).replace("\\", "/")
        for file in files:
            full_path = os.path.join(root, file)
            s3_key = join(wa, file)
            print(f"uploading {full_path} to {s3_key}")

            response = s3.put_object(Body=open(full_path, 'rb'),
                                     Bucket=bucket,
                                     ChecksumAlgorithm='SHA256',
                                     Key=s3_key)


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    upload_folder("c:\\temp\\uol\\payloads\\", "deposits/dev/dxfrsrjs/")
