// src/lib/faceModels.js
import * as faceapi from 'face-api.js'   // pulls in the face-api.js library

let loaded = false   // tracks whether models are already loaded, so we don't reload them every call
const FACE_DESCRIPTOR_LENGTH = 128

export async function loadFaceModels() {
  if (loaded) return   // skip loading if already done
  await Promise.all([
    faceapi.nets.tinyFaceDetector.loadFromUri('/models'),   // loads the face-locating model from public/models
    faceapi.nets.faceLandmark68Net.loadFromUri('/models'),  // loads the facial-landmark model
    faceapi.nets.faceRecognitionNet.loadFromUri('/models'), // loads the model that generates the 128-number descriptor
  ])
  loaded = true   // marks models as loaded so future calls skip straight to using them
}

export async function getFaceScan(videoOrImage) {
  const detection = await faceapi
    .detectSingleFace(videoOrImage, new faceapi.TinyFaceDetectorOptions())   // finds one face in the video/image
    .withFaceLandmarks()     // adds landmark points (eyes, nose, mouth) to the detection
    .withFaceDescriptor()    // adds the 128-number descriptor to the detection

  if (!detection) return null   // no face found, so return nothing

  const descriptor = Array.from(detection.descriptor, value => Number(value))
  const isValidDescriptor =
    descriptor.length === FACE_DESCRIPTOR_LENGTH &&
    descriptor.every(value => Number.isFinite(value))

  if (!isValidDescriptor) return null

  return {
    descriptor,
    landmarks: detection.landmarks.positions.map(point => ({ x: point.x, y: point.y }))
  }
}

export async function getFaceDescriptor(videoOrImage) {
  const scan = await getFaceScan(videoOrImage)
  return scan?.descriptor || null   // exactly 128 numeric values as a plain array, safe to send as JSON
}
