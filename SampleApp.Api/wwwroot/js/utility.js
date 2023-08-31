﻿
export default class Utility {

    constructor(elSpinner, elMessage) {
        this.elSpinner = elSpinner;
        this.elMessage = elMessage;
    }

    async HttpSend(method, url, data = null, contentType = "application/json",  parseResponseType = "json") {

        try {
            if (!contentType) contentType = "application/json";
            const options = {
                method: method, // *GET, POST, PUT, DELETE, etc.
                //mode: 'cors', // no-cors, *cors, same-origin
                //cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
                //credentials: 'same-origin', // include, *same-origin, omit
                headers: {
                    'Content-Type': contentType //outgoing type
                },
                //redirect: 'follow', // manual, *follow, error
                //referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
                body: (data) ? JSON.stringify(data) : null // body data type must match "Content-Type" header
            };
            this.toggleSpinner(true);
            const response = await fetch(url, options);
            if (response.ok) {
                switch (parseResponseType) {
                    case "json":
                        data = await response.json();
                        break;
                    case "text":
                        data = await response.text();
                        break;
                    default:
                        data = null;
                        break;
                }
                return { statusCode: response.status, data: data };
            }
            else {
                const err = await response.json();
                throw new Error(`${err.detail}`);
            }
        }
        catch (error) {
            console.error('HttpSend error:', error);
            this.elMessage.classList.add("error");
            this.elMessage.innerText = error;
            throw error;
        }
        finally {
            this.toggleSpinner(false);
        }
    }

    toggleSpinner = (toggle) => {
        if (toggle) this.elMessage.innerText = "";
        if (!this.elSpinner) return;
        toggle ? this.elSpinner.removeAttribute("hidden") : this.elSpinner.setAttribute("hidden", true);
    }

}