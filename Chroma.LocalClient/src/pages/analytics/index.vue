<script setup lang="ts">
import Card from "primevue/card";
</script>

<template>
    <div class="flex flex-column gap-2">
        <span class="text-4xl flex justify-content-start">Analytics</span>
        <hr />
        <Card>
            <template #content>
                <div
                    class="flex flex-column justify-content-start w-full align-items-start"
                >
                    <span class="font-semibold">Here you will be able to:</span>
                    <span> - view historical trends</span>
                    <span>
                        - view summarised information on each game (i.e.,
                        pass/failure rate, game pick rate, revenue per game,
                        etc.)
                    </span>
                    <span> - other future considerations</span>
                </div>
            </template>
        </Card>
    </div>
</template>

<style scoped lang="scss"></style>
