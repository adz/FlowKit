# Release Checklist

Use this checklist before cutting a public release.

## Documentation

- [ ] `README.md` still matches the current public API and package positioning
- [ ] main docs pages build cleanly through `bash scripts/generate-api-docs.sh`
- [ ] generated API reference renders correctly under `output/reference/`
- [ ] examples page still points to runnable example projects
- [ ] release notes include the version being shipped

## Examples

- [ ] main example still runs
- [ ] maintenance example still runs
- [ ] playground example still shows the smallest current surface honestly
- [ ] migration guide still reflects the recommended adoption path

## API Docs And Packaging

- [ ] `dotnet pack src/FsFlow/FsFlow.fsproj --configuration Release` succeeds
- [ ] package metadata points at `adz/FsFlow`
- [ ] packed `README.md` is still suitable for the NuGet package page
- [ ] symbol package is produced alongside the main package

## Semantic Edge Cases

- [ ] timeout behavior is still documented and covered by tests
- [ ] exception capture behavior is still documented and covered by tests
- [ ] cleanup on success, typed failure, and cancellation is still covered by tests
- [ ] retry attempt semantics are still documented and covered by tests

## Release

- [ ] CI is green on `main`
- [ ] GitHub Pages deployment is healthy
- [ ] release tag matches the package version
- [ ] release artifacts include `.nupkg` and `.snupkg`
- [ ] NuGet publish is completed
