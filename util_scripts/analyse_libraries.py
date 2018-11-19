import argparse
import xml.etree.ElementTree as ET
import re
import os
import shutil

LIBRARY_SEARCH_REGEX = r'(.*), ([\d.]*) \(([^\)]*)\)'
LIBRARY_XML_PATH = '*/Library/Name'
LIBRARY_FOLDER_NAME = "_Libraries"

# Get the tpy file from the user
ap = argparse.ArgumentParser()
ap.add_argument("-p", "--path", required=True, help="The project folder file to analyse for dependencies")
ap.add_argument("-d", "--delete", action="store_true", help="Removes folders that aren't in use")
args = vars(ap.parse_args())

project_name = os.path.basename(args["path"])

# Convert into xml
xml = ET.parse(os.path.join(args["path"], "{}.tpy".format(project_name)))
root = xml.getroot()

# Find the libraries
for library in root.findall('*/Library/Name'):
    library_text = library.text
    print("Checking library {}".format(library_text))
    # Extract version and path
    try:
        matches = re.search(LIBRARY_SEARCH_REGEX, library_text)
        library_name, version, folder = matches.group(1, 2, 3)
    except Exception as e:
        print("{}: format not recognised: {}".format(library_text, e))
        break

    # Confirm library exists
    root_path = os.path.join(args["path"], LIBRARY_FOLDER_NAME, folder, library_name)
    library_folder = os.path.join(root_path, version)
    if not os.path.isdir(library_folder):
        print("Library not found: {}".format(os.path))

    # List all versions that aren't being used
    unwanted_versions = [l for l in os.listdir(root_path) if
                         os.path.isdir(os.path.join(root_path, l)) and l != version]
    if unwanted_versions:
        print("Unused libraries found: {}".format(unwanted_versions))
        if args["delete"]:
            print("Deleting unwanted folders")
            for l in unwanted_versions:
                dir_path = os.path.join(root_path, l)
                shutil.rmtree(dir_path)
        print()

