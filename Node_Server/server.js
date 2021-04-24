const redis = require("redis");
const dgram = require("dgram");
const udpServer = dgram.createSocket("udp4");
const PORT = 9000;
const HOST = "127.0.0.1";
require("dotenv").config();
const db = require("./model/index");
const User = require("./model/User");
const sub = redis.createClient({
    host: "127.0.0.1",
    no_ready_check: true,
    auth_pass: process.env.REDIS_PASSWORD,
});
const pub = redis.createClient({
    host: "127.0.0.1",
    no_ready_check: true,
    auth_pass: process.env.REDIS_PASSWORD,
});

db();

udpServer.on("listening", () => {
    const address = udpServer.address();
    console.log("UDP Server listening on" + address.address);
});

udpServer.on("message", (message, remote) => {
    let recvData = JSON.parse(message.toString());
    switch (recvData.message) {
        case "loginRequest":
            function OnLogin(user) {
                if (user) {
                    const userInfo = {
                        nickname: user.nickname,
                        address: remote.address,
                        port: remote.port,
                    };
                    const message = JSON.stringify(userInfo);
                    pub.publish("Session", message);
                }
            }
            function UserNotFound() {
                console.log("없는 유저입니다.");
            }
            User.findOne({
                id: recvData.id,
                password: recvData.password,
            })
                .then(OnLogin)
                .catch(UserNotFound);
            break;
    }
});
udpServer.bind(PORT, HOST);

sub.subscribe("Session");
