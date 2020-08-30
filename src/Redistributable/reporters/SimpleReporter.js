const SimpleReporter = {
    jasmineStarted: function (suiteInfo) {
        console.log("Started\n");
    },

    jasmineDone: function (result) {
        console.log("\nEnded");
    },

    specDone: function (result) {
        //Insert dot between construction's name
        //Delete trailing spaces and replace remain spaces with underscores
        let suits = result.fullName.replace(result.description, ' ').trim().replace(/ /g, '_');
        let spec = result.description.trim().replace(/ /g, '_');
        console.log(suits + '.' + spec);
        console.log(result.status);
    }

};
jasmine.getEnv().clearReporters();
jasmine.getEnv().addReporter(SimpleReporter);