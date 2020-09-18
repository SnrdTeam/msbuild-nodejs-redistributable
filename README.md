# Preamble

This repository contains two packages. 
The [first package (`Adeptik NodeJs Redistributable`)](#Adeptik-NodeJs-Redistributable) contains the logic for delivering NodeJS, NPM, YARN and allows you 
to execute the scripts specified in the file `package.json` during build process uisng MSBuild.

The [second package (`Adeptik NodeJs UnitTesting TestAdapter`)](#Adeptik-NodeJs-UnitTesting-TestAdapter) contains a test adapter for MSBuild to run jasmine (BDD Javascript framework) unit test in your project. 
This package depends on the first.

---
# Adeptik NodeJs Redistributable

## About package

Target for MSBuild that installs NodeJS of specified version and yarn package manager. Also runs yarn command if specified.

MSBuild targets for:
* [NodeJs installation](#NodeJs-installation)
* [Yarn installation](#Yarn-installation)
* [Node modules installation via yarn](#Node-modules-installation-via-yarn)
* [Run node scripts from package.json via yarn](#Run-node-scripts-from-packagejson-via-yarn)
* [Generate jasmine configuration and launch settings file](#Generate-jasmine-configuration-and-launch-settings-file)

MSBuild tasks:
* NodeJS installation task (`InstallNodeJs`)
* Installation npm modules (`NPMInstallGlobal`)
* Generate jasmine configuration (`GenerateJasmineConfig`)

Supports Windows, Linux & MacOS.

![Build Status](https://tfs.adeptik.com/Adeptik/_apis/public/build/definitions/5f6da651-409b-4516-b0c6-16518d60e6e9/137/badge)
[![Nuget Package](https://img.shields.io/nuget/vpre/Adeptik.NodeJs.Redistributable)](https://www.nuget.org/packages/Adeptik.NodeJs.Redistributable/)

## Usage

### Package installation

Install Adeptik.NodeJs.Redistributable package from Nuget

    Install-Package Adeptik.NodeJs.Redistributable -Version 2.2.0

or add in your .csproj:

    <PackageReference Include="Adeptik.NodeJs.Redistributable" Version="2.2.0" />

### NodeJs installation

Specify a version of NodeJs you wish to use by adding property in .csproj:

```xml
<PropertyGroup>
    <NodeJsDistVersion>12.15.0</NodeJsDistVersion>
</PropertyGroup>
```

Now you can get downloaded distribution of NodeJs in your target by setting target's `DependsOn` attribute to `InstallNodeJs` and reading `$(NodeJsPath)`, `$(GlobalNodeModulesPath)`, `$(NodeExecutable)` and `$(NPMExecutable)` properties.

```xml
<Target Name="YourTarget" DependsOnTargets="InstallNodeJs">
    <Message Text="Using $(NodeJsPath)" />
</Target>
```
- `$(NodeJsPath)` contains full path to downloaded & unpacked NodeJs distrib (with trailing slash)
- `$(GlobalNodeModulesPath)` contains full path to node_modules directory where global packages are stored (with trailing slash)
- `$(NodeExecutable)` contains command (with full path) to run node executable
- `$(NPMExecutable)` contains command (with full path) to run npm (path to `npm-cli.js`)

### Yarn installation

You can install yarn package manager in your target by setting target's `DependsOn` attribute to `InstallYarn`. InstallYarn target depends on InstallNodeJs target. 

You can specify yarn version by setting `$(YarnVersion)` property. By default `$(YarnVersion)` set to `latest`. 

```xml
<PropertyGroup>
    <NodeJsDistVersion>12.15.0</NodeJsDistVersion>
    <YarnVersion>1.22.0</YarnVersion>
</PropertyGroup>
```
Yarn command stored to `$(YarnExecutable)` property.

### Node modules installation via Yarn

You can install node modules (listed in package.json file in the root of your project) using yarn package manager in your target by setting target's `DependsOn` attribute to `YarnInstall`. YarnInstall target depends on InstallYarn target.

### Run node scripts from package.json via Yarn

You can run node scripts described in package.json using Yarn by setting `$(YarnBuildCommand)`. Target `YarnRun` automatically runs this command before `CoreCompile`. Also note that YarnRun target depends on YarnInstall target. So you can set only two properties to get node, yarn, install node modules and run script, like shown below.

```xml
<PropertyGroup>
    <NodeJsDistVersion>12.15.0</NodeJsDistVersion>
    <YarnBuildCommand>build</YarnBuildCommand>
</PropertyGroup>
```

### Generate jasmine configuration and launch settings file

Target `GenerateJasmineConfigurationAndLaunchSettingsFile` will generate configuration file (`jasmine.json`) and file which contains launch settings of jasmine (jasmine launcher file [`JasmineExecutor.js`](./src/UnitTesting/scripts/JasmineExecutor.js), node executable file path and path to `jasmine.json`) in the output directory of your project. This target (`GenerateJasmineConfigurationAndLaunchSettingsFile`) depends on `InstallNodeJs` target.

Example of jasmine.json configuration:

```json
{
    "spec_dir": ".",
    "spec_files": 
    [
        "/path/to/out/dir/fail.test.js",
        "/path/to/out/dir/simple.test.js"
    ],
	"stopSpecOnExpectationFailure": "false",
	"random": "false"
}
```
File contains 4 parameters:
* `spec_dir` - Jasmine working directory. Always contain path to output directory with assembly.
* `spec_files` - Files in which the specs are located. The files should with the extension .test.js located in the same directory with the assembly.
* `stopSpecOnExpectationFailure` - Stop execution of a spec after the first expectation failure in it. Always false.
* `random` - Run specs in random order. Always false.

For more detail information about configuration file [click this](https://jasmine.github.io/setup/nodejs.html#configuration).

---

# Adeptik NodeJs UnitTesting TestAdapter 

## About package

This package provide infrastructure to run unit test using Jasmine javascript testing framework. It depends on Adeptik.NodeJs.Redistributable to run tests without NodeJS installed globally.

Supports Windows, Linux & MacOS.

![Build Status](https://tfs.adeptik.com/Adeptik/_apis/public/build/definitions/5f6da651-409b-4516-b0c6-16518d60e6e9/137/badge)
[![Nuget Package](https://img.shields.io/nuget/vpre/Adeptik.NodeJs.UnitTesting.TestAdapter)](https://www.nuget.org/packages/Adeptik.NodeJs.UnitTesting.TestAdapter)

## Usage

### Package installation

Install Adeptik.NodeJs.UnitTesting.TestAdapter package from Nuget

    Install-Package Adeptik.NodeJs.UnitTesting.TestAdapter -Version 1.1.0

or add in your .csproj:

    <PackageReference Include="Adeptik.NodeJs.UnitTesting.TestAdapter" Version="1.1.0" />

### How it works?

This package implements a mechanism for finding and executing test cases by creating test adapter for Jasmine testing (Test adapter use configuration file generated by target [`GenerateJasmineConfigurationAndLaunchSettingsFile`](#Generate-jasmine-configuration-and-launch-settings-file) from package "[`Adeptik NodeJs Redistributable`](#Adeptik-NodeJs-Redistributable)")

### Notes

* All jasmine test cases should be in the assembly output directory or its subdirectories.
* All test case output files must adhere to the following name format `[nameOfYourTestFile].test.js`. The name of the text file is indicated in brackets, which can be anything you like.
* All jasmine tests runs for test discovering.
* All jasmine tests runs when you try run exactly specified tests in test explorer.
* For correct duration measuring of the specs run, Jasmine version should be >= 3.6.0.
* Jasmine package should be located in the node_modules folder at the root of your test project before you run test. Do not forget to use package.json to install it.
