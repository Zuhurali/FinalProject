const assert = require("assert");
const path = require("path");
const api = require(path.resolve(process.argv[2]));

function digits(value) {
    return (value.match(/\d/g) || []).length;
}

assert.strictEqual(api.limitTeamMemberPhone(""), "");
assert.strictEqual(api.limitTeamMemberPhone("0503546767"), "0503546767");
assert.strictEqual(api.limitTeamMemberPhone("+994503546767"), "+994503546767");
assert.strictEqual(api.limitTeamMemberPhone("0503546767890"), "050354676789");
assert.strictEqual(digits(api.limitTeamMemberPhone("0503546767890")), 12);
assert.strictEqual(api.limitTeamMemberPhone("+9945035467673"), "+994503546767");
assert.strictEqual(api.limitTeamMemberPhone("+994503546767399999"), "+994503546767");
assert.strictEqual(api.limitTeamMemberPhone("0503546767abc"), "0503546767");
assert.strictEqual(api.limitTeamMemberPhone("+994503546767abc"), "+994503546767");
assert.strictEqual(api.limitTeamMemberPhone("abc"), "");

function Field(value) {
    this.value = value;
    this.selectionStart = value.length;
    this.selectionEnd = value.length;
    this.attributes = {};
    this.handlers = {};
    this.setSelectionRange = (start, end) => {
        this.selectionStart = start;
        this.selectionEnd = end;
    };
    this.addEventListener = (name, fn) => {
        this.handlers[name] = fn;
    };
    this.getAttribute = (name) => this.attributes[name] || null;
    this.setAttribute = (name, value) => {
        this.attributes[name] = value;
    };
    this.edit = (next, caret) => {
        this.value = next;
        this.selectionStart = caret == null ? next.length : caret;
        this.selectionEnd = this.selectionStart;
        this.handlers.input();
    };
}

const field = new Field("+994503546767");
api.bindTeamMemberPhone(field);

field.edit("", 0);
assert.strictEqual(field.value, "");

field.edit("0503546767", 10);
assert.strictEqual(field.value, "0503546767");

field.edit("+994503546767", 13);
assert.strictEqual(field.value, "+994503546767");

field.edit("+9945035467673", 14);
assert.strictEqual(field.value, "+994503546767");
assert.strictEqual(digits(field.value), 12);

field.edit("12345678901234567890", 20);
assert.strictEqual(digits(field.value), 12);
assert.strictEqual(field.value, "123456789012");

field.edit("+994503546767", 13);
field.edit("0503546767", 10);
assert.strictEqual(field.value, "0503546767");

console.log("ok");
