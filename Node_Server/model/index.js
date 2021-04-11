const mongoose = require("mongoose");
const User = require("./User");

const MONGO_URI = process.env.MONGO_URI;
module.exports = () => {
    function connect() {
        mongoose.connect(
            MONGO_URI,
            {
                dbName: "LoginDB",
            },
            (err) => {
                if (err) {
                    console.log(err);
                } else {
                    console.log("몽고디비 연결 성공!");
                }
            }
        );
    }
    connect();
    mongoose.connection.on("disconnected", connect);
    User;
};
