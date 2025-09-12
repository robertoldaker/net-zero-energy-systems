import os
import shutil

def rename_files(src, old_text, new_text):
    for root, dirs, files in os.walk(src,topdown=False):
        print(f'root={root}')
        for d in dirs:
            old_dn = os.path.join(root, d)
            new_d = d.replace(old_text, new_text)
            new_dn = os.path.join(root, new_d)
            if old_dn!=new_dn:
                shutil.move(old_dn,new_dn)

        # Replace file names
        for f in files:
            old_fn = os.path.join(root, f)
            new_f = f.replace(old_text, new_text)
            new_fn = os.path.join(root, new_f)
            if old_fn!=new_fn:
                shutil.move(old_fn,new_fn)

def replace_text_in_files( src, old_text, new_text ):
    for root, dirs, files in os.walk(src,topdown=False):
        print(f'root={root}')
        # Replace file names
        for f in files:
            fn = os.path.join(root, f)
            try:
                replace_text_in_file(fn,old_text,new_text)
            except Exception:
                continue

def replace_text_in_file(file_path, old_text, new_text):
    with open(file_path, 'r') as file:
        file_data = file.read()

    # Replace the target string
    file_data = file_data.replace(old_text, new_text)

    with open(file_path, 'w') as file:
        file.write(file_data)

# Example usage
#rename_files('ClientApp/src/app/loadflow', 'loadflow', 'boundcalc')
src_folder = 'ClientApp/src/app'
replace_text_in_files(src_folder, 'loadflow', 'boundcalc')
replace_text_in_files(src_folder, 'Loadflow', 'BoundCalc')
