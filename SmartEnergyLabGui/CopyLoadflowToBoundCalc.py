import os
import shutil

def copy_and_replace(src, old_text, new_text):
    for root, dirs, files in os.walk(src):
        print(f'root={root}')
        
        # Replace file names
        for f in files:
            new_root = root.replace(old_text,new_text)            
            #
            if not os.path.exists(new_root):
                os.makedirs(new_root)

            #
            fn = os.path.join(root, f)
            new_fn = os.path.join(new_root, f.replace(old_text,new_text))
            print(f'{fn}=>{new_fn}')
            shutil.copy(fn, new_fn)
            try:
                replace_text_in_file(new_fn,"loadflow","boundCalc")
                replace_text_in_file(new_fn,"Loadflow","BoundCalc")
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
src_folder = 'ClientApp/src/app/loadflow'
copy_and_replace(src_folder, 'loadflow', 'boundcalc')