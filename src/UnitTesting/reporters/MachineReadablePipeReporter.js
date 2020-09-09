const net = require('net');
const os = require('os');
const process = require('process');
const PIPE_NAME = 'ReporterJasminePipe' + process.ppid;
const PIPE_PATH = (os.platform == 'win32' ? '\\\\.\\pipe\\' : '/var/pipes/') + PIPE_NAME;

let stream = net.connect(PIPE_PATH).addListener("close", () => process.exit(0));

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
jasmine.getEnv().clearReporters();
jasmine.getEnv().addReporter(MachineReadablePipeReporter);
