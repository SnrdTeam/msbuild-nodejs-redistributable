const SimpleReporter = {

    //Empty
    jasmineStarted: function (suiteInfo) { },

    //Empty
    suiteStarted: function (result) { },

    //Empty
    specStarted: function (result) { },

    //Empty
    jasmineDone: function (result) { },

    //Add suite's name to array
    suiteDone: function (result) { },

    specDone: function (result) {
        //Insert dot between construction's name
        //Delete trailing spaces and replace remain spaces with underscores
        let suits = result.fullName.replace(result.description, ' ').trim().replace(/ /g, '_');
        let spec = result.description.trim().replace(/ /g, '_');

        console.log(suits + '.' + spec);
        console.log(result.status);
    }

};
jasmine.getEnv().addReporter(SimpleReporter);