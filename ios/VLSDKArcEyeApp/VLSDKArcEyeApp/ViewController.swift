import UIKit
import VLSDK
import MetalKit
import UniformTypeIdentifiers

class ViewController: UIViewController, UIDocumentPickerDelegate {
    private var session = VLSDKSession.shared()
    
    private var dragging: Bool = false
    
    private var metalView: MTKView!
    private var renderer: MetalRenderer?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Setup Metal View
        setupMetalView()
        
        // Setup renderer with Metal View
        if let renderer = MetalRenderer(mtkView: metalView) {
            self.renderer = renderer
            self.renderer?.hasChangedViewport = { width, height in
                self.session.setViewport(CGSizeMake(width, height))
            }
        }
        
        // Setup VLSDK session
        let config = VLSDKConfig(services: [
            VLSDKService(location: "VLSDK_ARCEYE_LOCATION_NAME",
                         invokeUrl: "VLSDK_ARCEYE_VL_API_URL",
                         secretKey: "VLSDK_ARCEYE_VL_API_KEY")
        ])
        
        config.targetFps = .fast
        config.logLevel = .warning
#if VLSDK_DATASET_SCHEME
        config.useDecoder = true
#else
        config.useDecoder = false
#endif
        config.useRaycast = true
        config.onUpdateStatus = { status in
            DispatchQueue.main.async {
                self.statusLabel.text = "\(self.convertVLStatusToString(status: status))"
                
                if(status == .initial) {
                    self.datasetInfoLabel.text = "N/A"
                }
            }
        }
        
        config.onUpdateTargetFps = { fps in
            print("Requires Metal View (\(fps) Hz)")
            self.metalView.preferredFramesPerSecond = Int(fps)
        }
        
        config.onUpdateFrame = { frame in
            self.renderer?.update(viewMatrix: frame.viewMatrix,
                                  projMatrix: frame.projMatrix,
                                  texTransform: frame.textureTransform,
                                  pixelBuffer: frame.capturedImage)
            
            let translation = frame.viewMatrix.inverse.columns.3
            
            let timestampText = "timestamp: \(frame.timestamp)\n"
            let poseText = String(
                format: "translation: %.3f %.3f %.3f\n",
                translation.x, translation.y, translation.z
            )
            let rotationText = String(format: "bearing: \(String(describing: frame.bearing))\n")

            DispatchQueue.main.async {
                self.frameInfoLabel.text = "\(timestampText)\(poseText)\(rotationText)"
            }
        }
        
        config.onUpdateDatasetInfo = { datasetInfo in
            DispatchQueue.main.async {
                self.datasetInfoLabel.text = "\(datasetInfo)"
            }
        }
        
        session.setup(with: config)
        
        // Setup UI components
        setupUIComponents()
    }
    
    @objc private func resumeButtonTapped() {
        session.resume()
        session.reset()
    }
    
    @objc private func pauseButtonTapped() {
        session.pause()
        session.reset()
    }
    
    @objc private func handleTap(_ gesture: UITapGestureRecognizer) {
        let viewPoint = gesture.location(in: self.metalView)
        let normalized = CGPoint(
            x: viewPoint.x / self.metalView.bounds.width,
            y: viewPoint.y / self.metalView.bounds.height
        )
        
        if let hit = self.session.raycast(normalized) {
            addHitObject(at: hit.position)
        }
    }
    
    private func addHitObject(at position: simd_float3) {
        renderer?.addCube(at: position)
    }

    
    // MARK: - Utility
    
    private func convertVLStatusToString(status: VLSDKStatus) -> String {
        switch status {
        case .initial:
            self.statusLabel.textColor = .black
            return "initial"
        case .notRecognized:
            self.statusLabel.textColor = .red
            return "notRecognized"
        case .vlPass:
            self.statusLabel.textColor = .systemGreen
            return "vlPass"
        case .vlFail:
            self.statusLabel.textColor = .red
            return "vlFail"
        @unknown default:
            self.statusLabel.textColor = .red
            return "unknown"
        }
    }
    
    // MARK: - UI
    private let playImage  = UIImage(systemName: "play.fill")
    private let pauseImage = UIImage(systemName: "pause.fill")
    
    private lazy var playPauseButton: UIButton = {
        let button = UIButton(type: .system)
        button.setImage(playImage, for: .normal)
        button.tintColor = .systemBlue
        return button
    }()
    
    private let browseButton: UIButton = {
        let button = UIButton(type: .system)
        button.setImage(UIImage(systemName: "folder.fill"), for: .normal)
        button.tintColor = .systemBlue
        return button
    }()
    
    private let seekBar: UISlider = {
        let slider = UISlider()
        slider.minimumValue = 0
        slider.maximumValue = 1
        slider.value = 0
        return slider
    }()
    
    private let resumeButton: UIButton = {
        let button = UIButton(type: .system)
        button.setTitle("Resume", for: .normal)
        button.translatesAutoresizingMaskIntoConstraints = false
        button.backgroundColor = UIColor.systemBlue
        button.setTitleColor(.white, for: .normal)
        button.layer.cornerRadius = 8
        return button
    }()
    
    private let pauseButton: UIButton = {
        let button = UIButton(type: .system)
        button.setTitle("Pause", for: .normal)
        button.translatesAutoresizingMaskIntoConstraints = false
        button.backgroundColor = UIColor.systemRed
        button.setTitleColor(.white, for: .normal)
        button.layer.cornerRadius = 8
        return button
    }()
    
    private let infoContainerView: UIView = {
        let view = UIView()
        view.translatesAutoresizingMaskIntoConstraints = false
        view.backgroundColor = UIColor.lightGray
        view.layer.cornerRadius = 12
        return view
    }()
    
    private let statusLabel: UILabel = {
        let label = UILabel()
        label.translatesAutoresizingMaskIntoConstraints = false
        label.text = "N/A"
        label.textColor = .black
        label.font = UIFont.systemFont(ofSize: 11, weight: .medium)
        return label
    }()
    
    private let datasetInfoLabel: UILabel = {
        let label = UILabel()
        label.translatesAutoresizingMaskIntoConstraints = false
        label.text = "N/A"
        label.textColor = .black
        label.font = UIFont.systemFont(ofSize: 11, weight: .medium)
        label.numberOfLines = 0
        return label
    }()
    
    private let frameInfoLabel: UILabel = {
        let label = UILabel()
        label.translatesAutoresizingMaskIntoConstraints = false
        label.text = "N/A"
        label.textColor = .black
        label.font = UIFont.systemFont(ofSize: 11, weight: .medium)
        label.numberOfLines = 0
        return label
    }()
    
    private func setupMetalView() {
        metalView = MTKView(frame: .zero)
        metalView.translatesAutoresizingMaskIntoConstraints = false
        
        view.addSubview(metalView)
        
        let topPadding: CGFloat = 20
        let sidePadding: CGFloat = 80
        
        NSLayoutConstraint.activate([
            metalView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: topPadding),
            metalView.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: sidePadding),
            metalView.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -sidePadding),
            metalView.heightAnchor.constraint(equalTo: metalView.widthAnchor, multiplier: 16.0/9.0)
        ])
        
        let tapGesture = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        metalView.addGestureRecognizer(tapGesture)
    }
    
    @objc private func onBrowseTapped(_ sender: UIButton) {
        if (session.decoder != nil) {
            session.pause()
            playPauseButton.setImage(playImage, for: .normal)
            
            let documentPicker = UIDocumentPickerViewController(forOpeningContentTypes: [.movie], asCopy: true)
            
            documentPicker.delegate = self
            present(documentPicker, animated: true)
        }
    }
    
    @objc private func onPlayPauseTapped(_ sender: UIButton) {
        if let decoder = session.decoder {
            if decoder.playing() {
                session.pause()
                sender.setImage(playImage, for: .normal)
            } else {
                session.resume()
                sender.setImage(pauseImage, for: .normal)
            }
        }
    }
    
    @objc private func sliderTouchDown(_ sender: UISlider) {
        dragging = true
    }

    @objc private func sliderTouchUpInside(_ sender: UISlider) {
        finishSeek(sender)
    }

    @objc private func sliderTouchUpOutside(_ sender: UISlider) {
        finishSeek(sender)
    }
    
    private func finishSeek(_ sender: UISlider) {
        guard let decoder = session.decoder else { return }

        session.pause()
        session.reset()

        if !decoder.playing() {
            session.resume()
            self.playPauseButton.setImage(self.pauseImage, for: .normal)
        }

        decoder.seek(Float(sender.value))
        dragging = false
    }
    
    func documentPicker(_ controller: UIDocumentPickerViewController, didPickDocumentsAt urls: [URL]) {
        if let decoder = session.decoder {
            guard let selectedURL = urls.first else { return }

            decoder.importDataset(selectedURL) { status in
                if(status == .success) {
                    print(selectedURL)
                    DispatchQueue.main.async {
                        self.session.resume()
                    }
                }
            }
            
            self.seekBar.minimumValue = 0
            self.seekBar.maximumValue = 1.0
            self.seekBar.value = 0
            self.playPauseButton.setImage(self.pauseImage, for: .normal)
            
            decoder.setOnProgress { progress in
                if(self.dragging == false) {
                    self.seekBar.value = progress
                }
            }
        }
    }
    
    private func setupUIComponents() {
        view.addSubview(infoContainerView)
        infoContainerView.addSubview(statusLabel)
        infoContainerView.addSubview(datasetInfoLabel)
        infoContainerView.addSubview(frameInfoLabel)
        
        let padding: CGFloat = 10
        let verticalSpacing: CGFloat = 20
        
        NSLayoutConstraint.activate([
            // Position the container view below the capturedImageView
            infoContainerView.topAnchor.constraint(equalTo: metalView.bottomAnchor, constant: verticalSpacing),
            infoContainerView.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 20),
            infoContainerView.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -20),
            
            // Status Label at the top of the container
            statusLabel.topAnchor.constraint(equalTo: infoContainerView.topAnchor, constant: padding),
            statusLabel.leadingAnchor.constraint(equalTo: infoContainerView.leadingAnchor, constant: padding),
            statusLabel.trailingAnchor.constraint(equalTo: infoContainerView.trailingAnchor, constant: -padding),
            
            // Dataset Info Label below Status Label
            datasetInfoLabel.topAnchor.constraint(equalTo: statusLabel.bottomAnchor, constant: padding),
            datasetInfoLabel.leadingAnchor.constraint(equalTo: infoContainerView.leadingAnchor, constant: padding),
            datasetInfoLabel.trailingAnchor.constraint(equalTo: infoContainerView.trailingAnchor, constant: -padding),
            
            // Frame Info Label below Dataset Info Label
            frameInfoLabel.topAnchor.constraint(equalTo: datasetInfoLabel.bottomAnchor, constant: padding),
            frameInfoLabel.leadingAnchor.constraint(equalTo: infoContainerView.leadingAnchor, constant: padding),
            frameInfoLabel.trailingAnchor.constraint(equalTo: infoContainerView.trailingAnchor, constant: -padding),
            frameInfoLabel.bottomAnchor.constraint(equalTo: infoContainerView.bottomAnchor, constant: -padding)
        ])
        
        if (session.decoder != nil) {
            // Dataset UI
            let controlStack = UIStackView(arrangedSubviews: [seekBar, playPauseButton, browseButton])
            controlStack.axis = .horizontal
            controlStack.alignment = .center
            controlStack.distribution = .fill
            controlStack.spacing = 16
            
            view.addSubview(controlStack)
            controlStack.translatesAutoresizingMaskIntoConstraints = false
            
            NSLayoutConstraint.activate([
                controlStack.topAnchor.constraint(equalTo: infoContainerView.bottomAnchor, constant: verticalSpacing),
                controlStack.leadingAnchor.constraint(equalTo: infoContainerView.leadingAnchor),
                controlStack.trailingAnchor.constraint(equalTo: infoContainerView.trailingAnchor),
                controlStack.heightAnchor.constraint(equalToConstant: 50)
            ])
            
            playPauseButton.setContentHuggingPriority(.required, for: .horizontal)
            browseButton.setContentHuggingPriority(.required, for: .horizontal)
            seekBar.setContentHuggingPriority(.defaultLow, for: .horizontal)
            
            seekBar.addTarget(self, action: #selector(sliderTouchDown(_:)), for: .touchDown)
            seekBar.addTarget(self, action: #selector(sliderTouchUpInside(_:)), for: .touchUpInside)
            seekBar.addTarget(self, action: #selector(sliderTouchUpOutside(_:)), for: .touchUpOutside)
            
            playPauseButton.addTarget(self, action: #selector(onPlayPauseTapped(_:)), for: .touchUpInside)
            browseButton.addTarget(self, action: #selector(onBrowseTapped(_:)), for: .touchUpInside)
        } else {
            // Camera UI
            resumeButton.addTarget(self, action: #selector(resumeButtonTapped), for: .touchUpInside)
            pauseButton.addTarget(self, action: #selector(pauseButtonTapped), for: .touchUpInside)
            
            view.addSubview(resumeButton)
            view.addSubview(pauseButton)
            
            let bottomPadding: CGFloat = 20
            // let buttonSpacing: CGFloat = 20
            let buttonWidth: CGFloat = 100
            let buttonHeight: CGFloat = 40
            
            NSLayoutConstraint.activate([
                // Resume Button at the bottom left
                resumeButton.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 40),
                resumeButton.bottomAnchor.constraint(equalTo: view.safeAreaLayoutGuide.bottomAnchor, constant: -bottomPadding),
                resumeButton.widthAnchor.constraint(equalToConstant: buttonWidth),
                resumeButton.heightAnchor.constraint(equalToConstant: buttonHeight),
                
                // Pause Button at the bottom right
                pauseButton.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -40),
                pauseButton.bottomAnchor.constraint(equalTo: view.safeAreaLayoutGuide.bottomAnchor, constant: -bottomPadding),
                pauseButton.widthAnchor.constraint(equalToConstant: buttonWidth),
                pauseButton.heightAnchor.constraint(equalToConstant: buttonHeight)
            ])
        }
    }
}
