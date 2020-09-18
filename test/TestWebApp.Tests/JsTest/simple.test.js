var sitejs = require('../../TestWebApp/wwwroot/js/site');

describe("SampleSuite", () => {
    describe("SampleTest", () => {
        it("should be ok", () => {
            expect(sitejs.add(2, 2)).toEqual(4);
        });
    });
});