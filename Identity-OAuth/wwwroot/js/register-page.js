"use strict"

import { bindAuthForm } from "./auth-form.js"
import { routes } from "./routes.js"

bindAuthForm("registerForm", routes.register)
