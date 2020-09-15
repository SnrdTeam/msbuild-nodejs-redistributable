const Jasmine = require('jasmine');
const net = require('net');
const process = require('process');
const os = require('os');
String.prototype.replaceAt = function(index, replacement) {
    return this.substr(0, index) + replacement + this.substr(index + replacement.length);
}
let config = process.argv[2];
let uniqueGuidIdentificator = process.argv[3];
const PIPE_NAME = 'ReporterJasminePipe' + uniqueGuidIdentificator;
const PIPE_PATH = (os.platform == 'win32' ? '\\\\.\\pipe\\' : '/tmp/CoreFxPipe_') + PIPE_NAME;

let stream = net.connect(PIPE_PATH).addListener("close", () => process.exit(1));

let jasmine = new Jasmine();

const MachineReadablePipeReporter = {
    specDone: function (result) {
        //Insert dot between suits and spec name. And replace all spaces symbols on underline char
        let name = result.fullName.replaceAt(result.fullName.indexOf(result.description) - 1, '.').replace(/\s+/g, '_');
        stream.write(name + os.EOL);
        stream.write(result.status + os.EOL);
    },

    jasmineDone: function (result) {
        stream.end();
    }
};

jasmine.clearReporters();
jasmine.addReporter(MachineReadablePipeReporter);
jasmine.loadConfigFile(config);
jasmine.execute();