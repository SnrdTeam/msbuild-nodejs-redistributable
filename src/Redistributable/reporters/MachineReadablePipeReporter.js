const net = require('net');
const os = require('os');
const PIPE_NAME = 'ReporterJasminePipe'
const PIPE_PATH = (os.platform == 'win32' ? '\\\\.\\pipe\\' : '/var/pipes/') + PIPE_NAME;

let stream = net.connect(PIPE_PATH);

const MachineReadablePipeReporter = {
    jasmineDone: function (result) {
        stream.end();
    },

    specDone: function (result) {
        //Insert dot between construction's name
        //Delete trailing spaces and replace remain spaces with underscores
        let suits = result.fullName.replace(result.description, ' ').trim().replace(/ /g, '_');
        let spec = result.description.trim().replace(/ /g, '_');
        stream.write(suits + '.' + spec + "\r\n");
        stream.write(result.status + "\r\n");
    }

};
jasmine.getEnv().clearReporters();
jasmine.getEnv().addReporter(MachineReadablePipeReporter);
