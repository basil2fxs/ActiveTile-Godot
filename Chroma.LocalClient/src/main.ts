import { createApp } from "vue";
import App from "./App.vue";
import "./style.css";
import "@/assets/styles.scss";
import PrimeVue from "primevue/config";
import DialogService from "primevue/dialogservice";
import ConfirmationService from "primevue/confirmationservice";
import ToastService from "primevue/toastservice";
import Tooltip from "primevue/tooltip";
import BadgeDirective from "primevue/badgedirective";
import Ripple from "primevue/ripple";
import StyleClass from "primevue/styleclass";
import Toast from "primevue/toast";
import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
import { setupLayouts } from "virtual:generated-layouts";
import generatedRoutes from "virtual:generated-pages";

const app = createApp(App);

// Generated routes are based on the file system structure. (https://github.com/hannoeru/vite-plugin-pages?tab=readme-ov-file)
// Layouts are set up for each route via the default.vue where the layout is not specifically defined. (https://github.com/JohnCampionJr/vite-plugin-vue-layouts)
const routes = setupLayouts(generatedRoutes);
const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

app.use(router);

// Add PrimeVue UI Framework. (https://primevue.org/)
app.use(PrimeVue, {
    ripple: true,
});

// Store and state management framework
app.use(createPinia());

// Add PrimeVue services which allows a variety of popups
app.component("Toast", Toast);
app.use(ToastService);
app.use(DialogService);
app.use(ConfirmationService);

// More PrimeVue features
app.directive("tooltip", Tooltip);
app.directive("badge", BadgeDirective);
app.directive("ripple", Ripple);
app.directive("styleclass", StyleClass);

app.mount("#app");
