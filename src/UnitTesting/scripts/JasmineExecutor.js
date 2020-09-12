const Jasmine = require("jasmine");
const net = require('net');
const process = require("process");
const os = require('os');
let config = process.argv[2];
let uniqueGuidIdentificator = process.argv[3];
const PIPE_NAME = 'ReporterJasminePipe' + uniqueGuidIdentificator;
const PIPE_PATH = (os.platform == 'win32' ? '\\\\.\\pipe\\' : '/tmp/CoreFxPipe_') + PIPE_NAME;

let stream = net.connect(PIPE_PATH).addListener("close", () => process.exit(1));

let jasmine = new Jasmine();

const MachineReadablePipeReporter = {
    specDone: function (result) {
        //Insert dot between construction's name
        //Delete trailing spaces and replace remain spaces with underscores
        let suits = result.fullName.replace(result.description, ' ').trim().replace(/ /g, '_').replace(/\n/g, '');
        let spec = result.description.trim().replace(/ /g, '_').replace(/\n/g, '');
        stream.write(suits + '.' + spec + os.EOL);
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