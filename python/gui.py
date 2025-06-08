import tkinter as tk
from tkinter import ttk, messagebox
from pathlib import Path
from download_mesh import download_mesh_grib2, grib2_to_tif
from main import process_tif


def run_pipeline():
    date = date_var.get().strip()
    time = time_var.get().strip() or "000000"
    product = product_var.get().strip() or "MESH_00.50"
    if not date:
        messagebox.showerror("Error", "Date is required (YYYYMMDD)")
        return
    try:
        grib2 = download_mesh_grib2(date, time, product)
        tif_dir = Path("simple_images")
        tif_dir.mkdir(parents=True, exist_ok=True)
        tif = tif_dir / f"{product}_{date}-{time}.tif"
        grib2_to_tif(grib2, tif)
        process_tif(str(tif))
        messagebox.showinfo("Success", f"Processed {tif}")
    except Exception as e:
        messagebox.showerror("Error", str(e))


def build_ui(root):
    root.title("MESH MAP")
    frm = ttk.Frame(root, padding=10)
    frm.grid()
    ttk.Label(frm, text="Date (YYYYMMDD)").grid(column=0, row=0, sticky="w")
    ttk.Entry(frm, textvariable=date_var).grid(column=1, row=0)
    ttk.Label(frm, text="Time (HHMMSS)").grid(column=0, row=1, sticky="w")
    ttk.Entry(frm, textvariable=time_var).grid(column=1, row=1)
    ttk.Label(frm, text="Product").grid(column=0, row=2, sticky="w")
    ttk.Entry(frm, textvariable=product_var).grid(column=1, row=2)
    ttk.Button(frm, text="Download & Process", command=run_pipeline).grid(column=0, row=3, columnspan=2, pady=5)


def main():
    root = tk.Tk()
    build_ui(root)
    root.mainloop()


date_var = tk.StringVar()
time_var = tk.StringVar(value="000000")
product_var = tk.StringVar(value="MESH_00.50")

if __name__ == "__main__":
    main()
