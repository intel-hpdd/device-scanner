module.exports = {
  print(x, serialize, indent) {
    try {
      const formatted = JSON.stringify(JSON.parse(x), null, 2);
      return formatted;
    } catch (e) {
      console.error("Error serializing", e);
      return x;
    }
  },

  test(x) {
    return x;
  },
};
