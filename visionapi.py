# import os;


# import numpy as np
# from keras.preprocessing import image
# from keras.applications.inception_v3 import InceptionV3, preprocess_input, decode_predictions
# from collections import OrderedDict





# #Load the InceptionV3 model pretrained on ImageNet data
# model = InceptionV3(weights='imagenet')
# def extract_features_and_labels(img_path):
#     # Load and preprocess the image
#     img = image.load_img(img_path, target_size=(299, 299))
#     img_array = image.img_to_array(img)
#     img_array = np.expand_dims(img_array, axis=0)
#     img_array = preprocess_input(img_array)

#     # Extract features using the InceptionV3 model
#     features = model.predict(img_array)
    
#     # Get the label predictions
#     label_predictions = decode_predictions(features, top=5)[0]
    
#     # Create a dictionary to store label predictions
#     labels_dict = OrderedDict()
#     for label in label_predictions:
#         labels_dict[label[1]] = label[2]
    
#     return features, labels_dict

# # Example usage:
# image_path = 'D:\download.jpg'
# features, label_predictions = extract_features_and_labels(image_path)

# print("Features shape:", features.shape)  # Shape of the extracted features
# print("Label predictions:")
# for label, confidence in label_predictions.items():
#     print(label, "-", confidence)  # Print label name and confidence

import onnx

# Load the ONNX model
model = onnx.load("D:\inception-v2-9.onnx")

# Get the names of all the output nodes
output_node_names = [output.name for output in model.graph.output]

print("Output Node Names:", output_node_names)
