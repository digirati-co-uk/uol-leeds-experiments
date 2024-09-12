# Preservation API Demonstration

These tests are not regular integration tests. Instead they use the Playwright test framework to demonstrate how to call the API for common scenarios.

The API is documented in this RFC:

https://github.com/uol-dlip/docs/blob/main/rfcs/003-preservation-api.md

There are three main tests and some helpers. In the same directory as this readme, run 

```
npm install
```

There are some sample digital objects in the samples directory, used by the tests. They comprise a METS file and some small JPEGS, to keep the tests small and quick.

### Create Deposit

`../tests/create-deposit.spec.ts`

This test demonstrates how to create a digital object in the repository from local files. Create a deposit, upload the files into the S3 location provided by the deposit, generate an import job, and run the import job.

Note that the API identifies the METS file and uses the SHA256 checksums it provides when it generates an import job. You do not need to provide them yourself.

```
npx playwright test create-deposit
```

### Exporting

`../tests/exporting.spec.ts`

This test shows how to export an existing digital object to S3, make some changes to the files in S3, ask the API to generate an Import Job, and then execute the import job. It shows the digital object in the repository moving from v1 to v2.

```
npx playwright test exporting
```

### Updating without exporting

`../tests/no-export-updates.spec.ts`

This test does the same as the previous test, except that it doesn't export the existing digital object. Suppose you just need to make a small change to one file, but the digital object contains 2GB of TIFFs. You can construct an import job manually and execute it without having to export first.

This test has two parts, and creates three separate versions of the same deposit.

```
npx playwright test no-export-updates
```

### Supporting files

`quick-prep.spec.ts`

This code does the same as the first create-deposit test, but in a more concise form (and includes a shortcut way of importing files). It is used by other tests to prepare an initial fixture.

`common-utils`

Helper functions to keep the main tests easier to read.