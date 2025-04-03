<script setup lang="ts">
// This is the root component. RouterView hooks with the Vue Router and other vite plugins, which handles which route this lands to.
// Since we are using the vite-plugin-pages, auto-generated routes is based on the 'pages' directory.
// As a result, the router will immediately land on the root 'index.vue' of the 'pages' directory.
</script>

<template>
    <Toast />
    <RouterView />
</template>

<style scoped></style>
