function fromBase64Url(value) {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(base64.length + ((4 - base64.length % 4) % 4), '=')
  const binary = atob(padded)
  return Uint8Array.from(binary, char => char.charCodeAt(0)).buffer
}

function toBase64Url(buffer) {
  const bytes = new Uint8Array(buffer)
  let binary = ''
  bytes.forEach(byte => { binary += String.fromCharCode(byte) })
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export function supportsPasskeys() {
  return Boolean(window.PublicKeyCredential && navigator.credentials)
}

export async function createPasskey(options) {
  if (!supportsPasskeys()) throw new Error('Passkeys are not supported by this browser or device.')

  const credential = await navigator.credentials.create({
    publicKey: {
      ...options,
      challenge: fromBase64Url(options.challenge),
      user: { ...options.user, id: fromBase64Url(options.user.id) },
      excludeCredentials: (options.excludeCredentials || []).map(credential => ({
        ...credential,
        id: fromBase64Url(credential.id)
      }))
    }
  })

  if (!credential) throw new Error('Passkey creation was cancelled.')
  return serializeCredential(credential)
}

export async function getPasskeyAssertion(options) {
  if (!supportsPasskeys()) throw new Error('Passkeys are not supported by this browser or device.')

  const credential = await navigator.credentials.get({
    publicKey: {
      ...options,
      challenge: fromBase64Url(options.challenge),
      allowCredentials: (options.allowCredentials || []).map(credential => ({
        ...credential,
        id: fromBase64Url(credential.id)
      }))
    }
  })

  if (!credential) throw new Error('Passkey verification was cancelled.')
  return serializeCredential(credential)
}

function serializeCredential(credential) {
  const response = credential.response
  const result = {
    id: credential.id,
    rawId: toBase64Url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: toBase64Url(response.clientDataJSON)
    },
    clientExtensionResults: credential.getClientExtensionResults?.() || {},
    authenticatorAttachment: credential.authenticatorAttachment || null
  }

  if ('attestationObject' in response) {
    result.response.attestationObject = toBase64Url(response.attestationObject)
    result.response.transports = response.getTransports?.() || []
  } else {
    result.response.authenticatorData = toBase64Url(response.authenticatorData)
    result.response.signature = toBase64Url(response.signature)
    result.response.userHandle = response.userHandle ? toBase64Url(response.userHandle) : null
  }

  return result
}
