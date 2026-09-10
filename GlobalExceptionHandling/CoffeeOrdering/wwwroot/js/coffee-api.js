import { requestJson } from "./http-client.js"

export function orderCoffee(drink) {
    return requestJson("/order", {
        method: "POST",
        body: JSON.stringify({ drink })
    })
}
