$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Update Password",
            content: "Remeber the new password.",
            icon: 'fa fa-key',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Update",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-password-bg");

                        // Update UI with a short delay to show spinner
                        setTimeout(function () {
                            $(".submit-progress-password").removeClass("hidden");
                        }, 1);
                        // Disable all buttons, submit inputs, and anchors
                        $('button, input[type="submit"], a').prop('disabled', true);

                        // Add a class to visually indicate disabled state for anchors
                        $('a').addClass('disabled-anchor').on('click', function (e) {
                            e.preventDefault(); // Prevent default action for anchor clicks
                        });
                        $('#updatebutton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Update");
                        form.submit();
                        var createForm = document.getElementById("edit-form");
                        if (createForm) {
                            const formElements = createForm.getElementsByTagName("*");
                            for (const element of formElements) {
                                element.disabled = true;
                                if (element.hasAttribute("readonly")) {
                                    element.classList.remove("valid", "is-valid", "valid-border");
                                    element.removeAttribute("aria-invalid");
                                }
                            }
                        }
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }
});

$(document).ready(function () {
    const currentpassword = $('#CurrentPassword');
    if (currentpassword) {
        currentpassword.focus()
    }

    $('#updatebutton').on('click', function (e) {
        $("#edit-form").addClass("submit-progress-bg");

        // Update UI with a short delay to show spinner
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#updatebutton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Update");
    });
    $("#edit-form").validate();
});
let typingInProgress = false;
let messageQueue = [];
const chatGPTMessage = document.getElementById('password-advise');

if (chatGPTMessage) {
    var userEmail = document.getElementById('Email').value;
    const eventSource = new EventSource(`/Session/StreamTypingUpdates?email=${encodeURIComponent(userEmail)}`);

    eventSource.onopen = () => console.log("SSE Connection Opened");
    eventSource.onerror = (e) => console.error("SSE Connection Failed", e);

    eventSource.addEventListener('message', (event) => {
        console.log("Received data:", event.data); // Debugging line

        if (event.data === "done") {
            eventSource.close();
        } else {
            messageQueue.push(event.data);
            processNextMessage();
        }
    });
}

// Function to process messages with typing effect
function processNextMessage() {
    if (typingInProgress || messageQueue.length === 0) {
        return;
    }
    typingInProgress = true;
    const message = messageQueue.shift();
    simulateTypingEffect(message, () => {
        typingInProgress = false;
        processNextMessage();
    });
}

// Function to simulate the typing effect
function simulateTypingEffect(message, callback) {
    const messageDiv = document.createElement('div');
    chatGPTMessage.appendChild(messageDiv);
    messageDiv.textContent = '';

    let index = 0;
    const typingInterval = setInterval(() => {
        if (index < message.length) {
            messageDiv.textContent += message[index];
            index++;
        } else {
            clearInterval(typingInterval);
            if (callback) callback();
        }
    }, 100);
}