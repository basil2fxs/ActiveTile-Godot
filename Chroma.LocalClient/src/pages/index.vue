<script lang="ts" setup>
import { useRouter } from "vue-router";

// Ideally this contains the landing page or a redirect to the login page.
// But for now in this template, direct to dashboard view.
// Each 'page' will use the 'layouts/default.vue' to determine the layout as defined by the vite-plugin-vue-layouts plugin.
// Note: the components in the 'layouts' come from the PrimeVue Sakai template (https://sakai.primevue.org/)
//       with a bit of tweaking to use auto-generated routes and layouts.
const router = useRouter();
router.push("/dashboard");
</script>

<template></template>
