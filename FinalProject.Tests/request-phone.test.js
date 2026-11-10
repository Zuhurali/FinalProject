const assert = require("assert");
const { limitNationalDigits } = require("../FinalProject.MVC/wwwroot/js/request-phone.js");

assert.strictEqual(limitNationalDigits("513183272222", 9, true), "513183272");
assert.strictEqual(limitNationalDigits("0516546565", 9, true), "516546565");
assert.strictEqual(limitNationalDigits("516546565", 9, true), "516546565");
assert.strictEqual(limitNationalDigits("555123456789", 10, false), "5551234567");
assert.strictEqual(limitNationalDigits("abc51 65", 9, true), "5165");

console.log("request-phone.test.js passed");
