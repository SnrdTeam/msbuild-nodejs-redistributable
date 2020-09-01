# Adeptik NodeJs Redistributable

## About package

Target for MSBuild that installs NodeJS of specified version and yarn package manager. Also runs yarn command if specified.

MSBuild targets for:
* NodeJs installation
* Yarn installation
* Node modules installation via yarn
* Run node scripts from package.json via yarn
* Jasmine installation
* Generate jasmine configuration and executable shell file

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
- `$(NPMExecutable)` contains command (with full path) to run npm

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

### Jasmine installation

If you want to use a jasmine testing framework, you may specify jasmine version by setting `$(JasmineVersion)`, otherwise jasmine will not be installed.

```xml
<PropertyGroup>
    <JasmineVersion>latest</JasmineVersion>
</PropertyGroup>
```

Jasmine command stored to `$(JasmineExecutable)` property. This target (`InstallJasmine`) depends on `InstallNodeJs`.

### Generate jasmine configuration and executable shell file

If you installed jasmine, in the output directory of your project will be generate configuration file (`jasmine.json`) and shell file which execute jasmine with custom reporter([`SimpleReporter.js`](./src/Redistributable/reporters/SimpleReporter.js)) and configuration file. This target (`GenerateJasmineConfigAndStartup`) depends on `InstallJasmine`.

Example of jasmine.json configuration:

```json
{
	"spec_dir": ".",
	"spec_files": [
			"/path/to/out/dir/fail.test.js",
			"/path/to/out/dir/simple.test.js"
		],
	"stopSpecOnExpectationFailure": "false",
	"random": "false"
}
```

For more detail information about configuration file [click this](https://jasmine.github.io/pages/docs_home.html).

# Adeptik NodeJs UnitTesting TestAdapter 

## About package

Infrastructure to run unit test using Jasmine javascript testing framework. Depends on Adeptik.NodeJs.Redistributable to run tests without NodeJS installed globally.

Supports Windows, Linux & MacOS.

![Build Status](https://tfs.adeptik.com/Adeptik/_apis/public/build/definitions/5f6da651-409b-4516-b0c6-16518d60e6e9/137/badge)
[![Nuget Package](https://img.shields.io/nuget/vpre/Adeptik.NodeJs.UnitTesting.TestAdapter)](https://www.nuget.org/packages/Adeptik.NodeJs.UnitTesting.TestAdapter)

## Usage

### Package installation

Install Adeptik.NodeJs.UnitTesting.TestAdapter package from Nuget

    Install-Package Adeptik.NodeJs.UnitTesting.TestAdapter -Version 1.1.0

or add in your .csproj:

    <PackageReference Include="Adeptik.NodeJs.UnitTesting.TestAdapter" Version="1.1.0" />

### How it's work?

This package implements a standard mechanism for finding and executing test cases by creating your own test adapter for Jasmine testing.

### Require:

* At build time, all jasmine test cases should be in the assembly output directory
* All test case output files must adhere to the following name format `nameOfYourTestFile.test.js`