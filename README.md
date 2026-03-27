# Blackbird.io Intento

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

The Intento app enables Blackbird workflows to use Intento for machine translation, translation quality review, OCR, text intelligence, and usage analytics.

### Translation

- **Translate text**: Translates text from one language to another.
- **Translate**: Translates files using one of two strategies: `blackbird` for interoperable translation workflows and `intento` for native document translation.
  Native Intento strategy supports:
  `TXT`, `HTML`, `HTM`, `XML`, `SRT`, `ICU`, `PDF`, `DOCX`, `XLSX`, `PPTX`, `PNG`, `JPG`, `JPEG`, `BMP`, and `ZIP`.
  When a file format is not supported by the native Intento strategy, the app automatically falls back to the `blackbird` strategy.
  For XLIFF/XLF files, the app uses native Intento only when the file can be mapped to `xliff-2.0`; otherwise it falls back to `blackbird`.

### Review

- **Review text**: Review translation quality for source and target text using IntentoQA.
- **Review**: Review translation quality using IntentoLQA for bilingual files, marks segments above the threshold as final, and returns the reviewed file together with summary metrics.

### Usage

- **Get usage statistics**: Retrieves aggregated usage statistics from Intento for either Intento usage or provider usage endpoints.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
