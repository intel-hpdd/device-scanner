module.exports = {
  print(x, serialize, indent) {
    try {
      return JSON.stringify(JSON.parse(x), null, 2);
    } catch (e) {
      console.error("Error serializing", e);
      return x;
    }
  },

  test(x) {
    return x;
  },
};
