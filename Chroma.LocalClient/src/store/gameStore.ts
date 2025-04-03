import { defineStore } from "pinia";

export type GameDefinition = {
    id: string;
    displayName: string;
    description: string;
};

// Store dedicated for game definitions, and later any session-scoped data.
// This store composable allows access to the game definitions and state which persist throughout the application
export const useGameStore = defineStore("gameStore", () => {
    // TODO - hardcoded game definitions for now. Will be pulled from server later
    return {
        games: (): GameDefinition[] => {
            return [
                {
                    id: "snake-game",
                    displayName: "Snake",
                    description: "Snake Description",
                },
                {
                    id: "breakout-game",
                    displayName: "Breakout",
                    description: "Breakout Description",
                },
                {
                    id: "color-domination-game",
                    displayName: "Color Domination",
                    description: "Color Domination Description",
                },
            ];
        },
    };
});
