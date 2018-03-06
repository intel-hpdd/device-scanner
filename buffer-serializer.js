const buffer = require('buffer');

module.exports = {
  print(x, serialize, indent) {
    const val = x.toString();
    try {
      return serialize(JSON.stringify(JSON.parse(val), null, 2));
    } catch (e) {
      return serialize(val);
    }
  },

  test(x) {
    return x && buffer.Buffer.isBuffer(x);
  },
};
