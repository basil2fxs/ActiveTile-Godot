import { useToast } from "primevue/usetoast";
import { useConfirm } from "primevue/useconfirm";

export function usePopup() {
  const toast = useToast();
  const confirm = useConfirm();
  return {
    error: (message: string) => {
      toast.add({
        severity: "error",
        summary: "Error",
        detail: message,
        life: 3000,
      });
    },
    success: (message: string) => {
      toast.add({
        severity: "success",
        summary: "Success",
        detail: message,
        life: 3000,
      });
    },
    warn: (message: string) => {
      toast.add({
        severity: "warn",
        summary: "Warning",
        detail: message,
        life: 3000,
      });
    },
    info: (message: string) => {
      toast.add({
        severity: "info",
        summary: "Info",
        detail: message,
        life: 3000,
      });
    },
    confirm: async (message: string, target: any): Promise<boolean> => {
      return new Promise((resolve, reject) => {
        confirm.require({
          target: target,
          message: message,
          icon: "pi pi-exclamation-triangle",
          accept: () => {
            resolve(true);
          },
          reject: () => {
            resolve(false);
          },
        });
      });
    },
    infoPopup: async (
      header: string,
      message: string,
      target: any,
    ): Promise<boolean> => {
      return new Promise((resolve, reject) => {
        confirm.require({
          target: target,
          header: header,
          message: message,
          icon: "pi pi-info-circle",
          acceptLabel: "Ok",
          rejectLabel: "",
          rejectClass: "display-none",
          accept: () => {
            resolve(true);
          },
          reject: () => {
            resolve(false);
          },
        });
      });
    },
  };
}
