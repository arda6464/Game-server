import os
import glob

base_dir = r"c:\Project\Game-server\src\Message\Packet"

def process_directory(directory):
    request_files = glob.glob(os.path.join(directory, "*RequestPacket.cs"))
    for req_file in request_files:
        basename = os.path.basename(req_file).replace("RequestPacket.cs", "")
        res_file = os.path.join(directory, f"{basename}ResponsePacket.cs")
        
        if os.path.exists(res_file):
            print(f"Merging {basename} in {os.path.basename(directory)}")
            
            with open(req_file, 'r', encoding='utf-8-sig') as f:
                req_lines = f.readlines()
            with open(res_file, 'r', encoding='utf-8-sig') as f:
                res_lines = f.readlines()
                
            usings = []
            body = []
            
            for line in req_lines:
                if line.strip().startswith("using "):
                    if line not in usings:
                        usings.append(line)
                else:
                    body.append(line)
                    
            body.append("\n") # Add a newline between classes
            
            for line in res_lines:
                if line.strip().startswith("using "):
                    if line not in usings:
                        usings.append(line)
                else:
                    body.append(line)
                    
            new_file = os.path.join(directory, f"{basename}Packets.cs")
            with open(new_file, 'w', encoding='utf-8-sig') as f:
                f.writelines(usings)
                f.write("\n")
                f.writelines(body)
                
            os.remove(req_file)
            os.remove(res_file)

if __name__ == "__main__":
    for root, dirs, files in os.walk(base_dir):
        process_directory(root)
    print("Merge completed.")
