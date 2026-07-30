import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: "#F4EFE7",
        cream: "#10151D",
        surface: {
          DEFAULT: "#1A222D",
          raised: "#222D3A",
          input: "#121A24",
        },
        brass: {
          DEFAULT: "#D9A766",
          dark: "#B9793E",
          light: "#F0C78C",
        },
        steel: {
          DEFAULT: "#AAB4C1",
          light: "#718093",
        },
        confirmed: "#74C69D",
        cancelled: "#F08080",
        completed: "#8EBAF5",
        noshow: "#E8C071",
      },
      fontFamily: {
        display: ["Fraunces", "Georgia", "serif"],
        sans: ["Inter", "system-ui", "sans-serif"],
      },
      borderRadius: {
        sm: "8px",
        md: "12px",
        lg: "20px",
      },
    },
  },
  plugins: [],
};

export default config;
