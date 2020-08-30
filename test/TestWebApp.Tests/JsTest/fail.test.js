describe("SampleSuite2", () => {
    describe("SampleTest2", () => {
        it("should fail", () => {
            expect(true).toEqual(false);
        });
	it("and so is a spec", function() {
    	var a = true;

    	expect(a).toBe(true);
  	});
    });
});