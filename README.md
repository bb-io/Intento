# Blackbird.io Intento

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

The Intento app enables Blackbird workflows to use Intento for machine translation, translation quality review, OCR, text intelligence, and usage analytics.

### Translation

- **Translate text**: Translates text from one language to another.
- **Translate**: Translates files using one of two strategies: `blackbird` for segment-based interoperable translation workflows and `intento` for native document translation. Supported native formats include TXT, HTML, HTM, XML, and CSV. XLIFF/XLF files are handled through the Blackbird strategy.

### Review

- **Review text**: Reviews the quality of the provided source and target text and returns a score.
- **Review**: Reviews translation quality for bilingual files, marks segments above the threshold as final, and returns the reviewed file together with summary metrics.

### Content intelligence

- **Extract text from an image**: Extracts text from an uploaded image using OCR.
- **Get sentiment of text**: Analyzes the sentiment of a given text.
- **Get meanings of text**: Retrieves dictionary meanings for the provided text.
- **Classify text**: Classifies text into predefined categories.

### Usage

- **Get usage statistics**: Retrieves aggregated usage statistics from Intento for either Intento usage or provider usage endpoints.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
