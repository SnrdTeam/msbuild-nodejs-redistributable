let Jas = require('jasmine');
let jasmine = new Jas.Jasmine();

//cmd's argument(path to config)
jasmine.loadConfigFile(process.argv[2]);

let SimpleReporter = {
    jasmineStarted: function(suiteInfo) {
        //Empty
    },
  
    suiteStarted: function(result) {
        //Empty
    },
  
    specStarted: function(result) {
        //EmptyK
    },
  
    specDone: function(result) {
      console.log('Spec: ' + result.description + ' was ' + result.status);
      
      for(let i = 0; i < result.failedExpectations.length; i++) {
        console.log('Failure: ' + result.failedExpectations[i].message);
        console.log(result.failedExpectations[i].stack);
      }
  
      console.log(result.passedExpectations.length);
    },
  
    suiteDone: function(result) {
      console.log('Suite: ' + result.description + ' was ' + result.status);
      for(let i = 0; i < result.failedExpectations.length; i++) {
        console.log('Suite ' + result.failedExpectations[i].message);
        console.log(result.failedExpectations[i].stack);
      }
    },
  
    jasmineDone: function(result) {
        //Empty
    }
  };
  
  jasmine.getEnv().addReporter(SimpleReporter);
  jasmine.execute();