const mongoose = require("mongoose");

const userSchema = new mongoose.Schema({
    id: {
        type: String,
        unique: false,
    },
    password: {
        type: String,
    },
});

const User = mongoose.model("User", userSchema);

module.exports = User;
