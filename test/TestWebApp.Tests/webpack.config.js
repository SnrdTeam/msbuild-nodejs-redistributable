const path = require('path');
const fs = require('fs');

module.exports = {
    mode: 'none',
    target: 'node',
    entry: () => {
        var entries = {};
        var files = fs.readdirSync(path.join(__dirname, "JsTest"));
        files.forEach(file => {
            var name = path.basename(file, ".js");
            entries[name] = `./JsTest/${file}`;
        })
        console.log(entries);
        return entries;
    },
    output: {
        path: path.resolve(__dirname, 'bin/Debug/netcoreapp3.1')
    }

};