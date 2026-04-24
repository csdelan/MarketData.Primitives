# DEPENDENCIES.md

## Published dependency guidance

This repository stays isolated from dependency source repositories when possible. For dependencies resolved from `$(BlueSkiesOutput)`, use the published documentation that ships with the dependency instead of pulling source into this repo unless the published notes are insufficient.

## Core

- Assembly: `$(BlueSkiesOutput)\Core\Core.dll`
- Published usage notes: `$(BlueSkiesOutput)\Core\README.md`
- Purpose in this repository: provides shared base types such as `Core.ValueObject` used by market-data domain models.
- Guidance: when work depends on Core types or behavior, read the published README first and keep changes in this repository aligned with that published contract.
