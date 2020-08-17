var SimpleReporter = {

    //Empty
    jasmineStarted: function (suiteInfo) { },

    //Empty
    suiteStarted: function (result) { },

    //Empty
    specStarted: function (result) { },

    //Empty
    jasmineDone: function (result) { },

    specDone: function (result) {
        console.log('Spec: ' + result.description);
        console.log('Result: ' + result.status);
    },

    suiteDone: function (result) {
        console.log('Suite: ' + result.description);
        console.log('Result: ' + result.status);
    }
};


jasmine.getEnv().addReporter(SimpleReporter);