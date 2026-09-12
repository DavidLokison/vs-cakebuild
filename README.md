# Vintage Story CakeBuild

This repository contains a preconfigured, modular build helper for Vintage Story powered by [cake build](https://cakebuild.net). Its aim is to be used from within a subdirectory of the mod project, preferably via being a git submodule, so that it can be updated alongside the mod project and doesn't have to be duplicated as much.

## Template
This submodule is part of a mod coding template (link still missing cause I am not sure on how to structure my templates).

## Usage
- run `git submodule add https://github.com/DavidLokison/vs-cakebuild.git build` inside your project root
- make sure you add `build//**` and `dist//**` to your default excludes in the mod project
- use `dotnet run --project build` to build and package your mod

This will then produce a mod directory for testing and the packaged ZIP file inside the `dist` directory of your project root.

## Motivation
I mainly wanted to strip down the mod template as far as possible so a submodule for the "usual" cake build made sense to me. This then implied some project changes cause we cannot hardcode a mod name any longer but have to derive it from the project itself. The next hurdle was flattening the project structure as dotnet is perfectly capable of deriving the name from the CSPROJ file, not needing an actual project name in the directory.

## Thanks
This project is heavily inspired (and mainly derived from) the [Vintage Story Mod Template](https://github.com/anegostudios/vsmodtemplate) from Anego Studios put under CC0.
