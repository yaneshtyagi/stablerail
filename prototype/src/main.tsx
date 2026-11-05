import { createRoot } from "react-dom/client";
import {
  AppRoot,
  ThemeProvider,
  ToastProvider,
} from "@yaneshtyagi/design-system";
import "@yaneshtyagi/design-system/fonts.css";
import "@yaneshtyagi/design-system/styles.css";
import "./styles.css";
import { App } from "./App";
createRoot(document.getElementById("root")!).render(
  <ThemeProvider>
    <ToastProvider>
      <AppRoot>
        <App />
      </AppRoot>
    </ToastProvider>
  </ThemeProvider>,
);
