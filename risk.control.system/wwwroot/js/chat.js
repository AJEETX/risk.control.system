const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", (user, message) => {
    const msg = `${user}: ${message}`;
    const li = document.createElement("li");
    li.textContent = msg;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("UserConnected", (user) => {
    const msg = `${user} connected`;
    const li = document.createElement("li");
    li.textContent = msg;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("UserDisconnected", (user) => {
    const msg = `${user} disconnected`;
    const li = document.createElement("li");
    li.textContent = msg;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("UpdateUsers", (users) => {
    const usersList = document.getElementById("usersList");
    usersList.innerHTML = "";
    users.forEach(user => {
        const li = document.createElement("li");
        li.textContent = user;
        usersList.appendChild(li);
    });
});

connection.start().then(() => {
    const userElement = document.getElementById("userInput");
    const user = userElement.value;
    //document.getElementById("userInput").value = user;
    connection.invoke("SendMessage", user, "joined");
}).catch(err => console.error(err.toString()));

document.getElementById("sendButton").addEventListener("click", () => {
    const user = document.getElementById("userInput").value;
    const message = document.getElementById("messageInput").value;
    document.getElementById("messageInput").value = '';
    connection.invoke("SendMessage", user, message).catch(err => console.error(err.toString()));
});