const redis = require("redis");
const dgram = require("dgram");
const udpServer = dgram.createSocket("udp4");
const PORT = 8000;
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
                    //redis publish
                    sub.on("subscribe", (channel, count) => {
                        pub.publish("Session", "첫 번째 메시지");
                    });
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
    }
});

udpServer.bind(PORT, HOST);

sub.on("subscribe", (channel, count) => {
    const user = {
        id: "이데아",
        address: "127.0.0.1",
        port: "8000",
    };
    const message = JSON.stringify(user);
    pub.publish("Session", message);
});

// sub.on("message", (channel, message) => {
//     console.log("채널명" + channel + "메시지" + message);
//     msg_count++;

//     if (msg_count == 3) {
//         sub.unsubscribe();
//         sub.end;
//         pub.end;
//     }
// });

sub.subscribe("Session");
