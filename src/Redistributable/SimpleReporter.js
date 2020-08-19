let suitesAndSpecs = [];
var SimpleReporter = {
    //Empty
    jasmineStarted: function (suiteInfo) { },

    //Empty
    suiteStarted: function (result) { },

    //Empty
    specStarted: function (result) { },

    //Empty
    jasmineDone: function (result) { },

    //Add suite's name to array
    suiteDone: function (result) {
        suitesAndSpecs.push(result.name);
    },

    specDone: function (result) {
        //Get full name of spec's
        let name = result.fullName;
        //Insert dot between construction's name
        for (var i = 0; i < suitesAndSpecs.length; ++i) {
            let indexSubStr = name.indexOf(suitesAndSpecs[i]);
            if (indexSubStr != -1) {
                name[indexSubStr + suitesAndSpecs[i].length] = ".";
            }
        }
        //Delete trailing spaces and replace remain spaces with underscores
        console.log(name.trim().replace(' ', '_'));
        console.log(result.status);
    }

};

jasmine.getEnv().addReporter(SimpleReporter);