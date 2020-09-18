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
        //Default write UTF-8
        stream.write(JSON.stringify(result, null, 0) + os.EOL, 'utf8');
    },

    jasmineDone: function (result) {
        //Write NULL character
        stream.write(String.fromCharCode(0) + os.EOL);
        stream.end();
    }
};

jasmine.clearReporters();
jasmine.addReporter(MachineReadablePipeReporter);
jasmine.loadConfigFile(config);
jasmine.execute();